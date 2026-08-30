using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoOS.App.Data.Contexts;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Services.Bios;

public sealed class BiosBackupService(IBiosSettingsContext context, IBiosNvramService nvramService, IBiosInfoService infoService) : IBiosBackupService
{
	private static readonly JsonSerializerOptions BackupJsonOptions = new()
	{
		TypeInfoResolver = BackupJsonContext.Default,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true,
		IndentCharacter = '\t',
		IndentSize = 1,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	public string? LastDriverError { get; private set; }

	public string BackupDirectory => Path.Combine(PathHelper.GetAppDataFolderPath(), "BIOS Settings", "Backups");

	public async Task BackupAsync(List<Setting> settings)
	{
		List<BackupSetting> currentSettings = [.. settings.Select(setting =>
		{
			bool hasRange = setting.Options.Count == 0 && (setting.Minimum != 0 || setting.Maximum != 0);

			return new BackupSetting
			{
				Path = setting.Path,
				Setting = setting.Name,
				Description = setting.Description,
				Minimum = hasRange ? setting.Minimum : null,
				Maximum = hasRange ? setting.Maximum : null,
				Increment = hasRange ? setting.Increment : null,
				Value = SettingState.GetDisplayValue(setting, setting.Value),
				Options = [.. setting.Options.Select(o => o.Label)],
				Default = setting.Default,
				VariableName = setting.VariableName,
				VariableGuid = HiiHelper.GetGuidString(setting.VariableGuid),
				Attributes = GetEfiVariableAttributeNames(setting.VarAttributes),
				Offset = setting.Offset,
				Width = setting.Width,
				Token = setting.Token
			};
		})];

		string latest = string.Empty;
		if (Directory.Exists(BackupDirectory))
		{
			foreach (string file in Directory.EnumerateFiles(BackupDirectory, "*.json"))
			{
				if (string.Compare(Path.GetFileName(file), Path.GetFileName(latest), StringComparison.Ordinal) > 0)
					latest = file;
			}
		}

		if (latest.Length > 0 && context.LastBackupSettings == null)
		{
			await using FileStream latestFs = File.OpenRead(latest);
			BackupFile? previous = await JsonSerializer.DeserializeAsync(latestFs, BackupJsonContext.Default.BackupFile);
			context.LastBackupSettings = previous?.Settings;
		}

		if (latest.Length > 0 && context.LastBackupSettings != null && SettingsEqual(context.LastBackupSettings, currentSettings))
			return;

		Directory.CreateDirectory(BackupDirectory);

		var backup = new BackupFile
		{
			CreatedAt = DateTimeOffset.Now,
			BoardManufacturer = infoService.Info.BaseboardManufacturer,
			BoardProduct = infoService.Info.BaseboardProduct,
			BiosVersion = infoService.Info.BiosVersion,
			BiosVersionDate = infoService.Info.BiosReleaseDate,
			Settings = currentSettings
		};

		string path = Path.Combine(BackupDirectory, $"{DateTime.Now.ToLocalTime():yyyy-MM-dd_HH-mm-ss}.json");
		await using FileStream fs = File.Create(path);
		await JsonSerializer.SerializeAsync(fs, backup, BackupJsonContext.Default.BackupFile);

		context.LastBackupSettings = currentSettings;
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		await using FileStream fs = File.OpenRead(filePath);
		BackupFile? backup = await JsonSerializer.DeserializeAsync(fs, BackupJsonContext.Default.BackupFile);
		if (backup == null)
			return infoService.GetWriteProtectedState();

		LastDriverError = null;

		(PageMode Result, bool Failed) = await Task.Run(() =>
		{
			using AmiSmmTransport transport = new();
			if (!transport.TryLoad())
			{
				LastDriverError = transport.LastLoadError;
				return (PageMode.DriverLoadFailed, true);
			}

			if (!transport.TryInitSmm())
			{
				LastDriverError = transport.LastInitError ?? transport.LastLoadError;
				return (PageMode.DriverLoadFailed, true);
			}

			Dictionary<(string VariableName, Guid Guid, uint Offset), Setting> settingsByKey = [with((context.LastSettings?.Count ?? 0))];
			if (context.LastSettings != null)
			{
				foreach (Setting s in context.LastSettings)
					settingsByKey[(s.VariableName, s.VariableGuid, s.Offset)] = s;
			}

			bool anyFailed = false;

			foreach (IGrouping<(string Name, Guid Guid), BackupSetting> group in backup.Settings.GroupBy(static setting => (Name: setting.VariableName, Guid: Guid.TryParse(setting.VariableGuid, out Guid guid) ? guid : Guid.Empty)))
			{
				List<KeyValuePair<Setting, SettingState>> pairs = [with(group.Count())];
				foreach (BackupSetting backupSetting in group)
				{
					if (string.IsNullOrEmpty(backupSetting.Value))
						continue;

					if (!Guid.TryParse(backupSetting.VariableGuid, out Guid parsedGuid))
						continue;

					if (settingsByKey.TryGetValue((backupSetting.VariableName, parsedGuid, backupSetting.Offset), out Setting? current))
						pairs.Add(new KeyValuePair<Setting, SettingState>(current, new SettingState { Value = backupSetting.Value }));
				}

				if (pairs.Count == 0)
					continue;

				if (!nvramService.PatchVariable(pairs, out byte[]? patched, out uint attributes, transport) || patched == null)
				{
					anyFailed = true;
					continue;
				}

				if (nvramService.TryGetCurrentBlob(pairs[0].Key, out byte[]? currentBlob, out _, transport) && currentBlob != null && currentBlob.AsSpan().SequenceEqual(patched))
					continue;

				if (!transport.TrySetVariable(group.Key.Name, group.Key.Guid, attributes, patched, out uint _))
					anyFailed = true;
			}

			return (PageMode.Loaded, anyFailed);
		});

		if (Result == PageMode.DriverLoadFailed)
			return PageMode.DriverLoadFailed;

		return Failed ? infoService.GetWriteProtectedState() : PageMode.Loaded;
	}

	private static List<string> GetEfiVariableAttributeNames(uint attributes)
	{
		if (attributes == 0xFFFFFFFF || attributes == 0)
			return [];

		var flags = (EfiVariableAttributes)attributes;
		List<string> names = [];

		foreach (EfiVariableAttributes value in Enum.GetValues<EfiVariableAttributes>())
		{
			if (value == EfiVariableAttributes.None)
				continue;

			if (flags.HasFlag(value))
				names.Add(value.ToString());
		}

		return names;
	}

	private static bool SettingsEqual(List<BackupSetting> previous, List<BackupSetting> current)
	{
		if (previous.Count != current.Count)
			return false;

		Dictionary<(string VariableName, string VariableGuid, uint Offset), string> previousMap = [with(previous.Count)];
		foreach (BackupSetting p in previous)
			previousMap[(p.VariableName, p.VariableGuid.ToUpperInvariant(), p.Offset)] = p.Value;

		foreach (BackupSetting setting in current)
		{
			if (!previousMap.TryGetValue((setting.VariableName, setting.VariableGuid.ToUpperInvariant(), setting.Offset), out string? prevValue) || !string.Equals(prevValue, setting.Value, StringComparison.Ordinal))
				return false;
		}

		return true;
	}
}
