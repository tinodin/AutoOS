using System.IO.Compression;
using System.Text.Encodings.Web;
using AutoOS.App.Data.Contexts.Bios;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Services.Bios;

public sealed class BiosBackupService(IBiosSettingsContext context, IBiosNvramService nvramService, IBiosInfoService infoService) : IBiosBackupService
{
	internal static readonly JsonSerializerOptions BackupJsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true,
		IndentCharacter = '\t',
		IndentSize = 1,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	internal static readonly BackupJsonContext BackupJsonContextRelaxed = new(BackupJsonOptions);

	public string? LastDriverError { get; private set; }

	public string BackupDirectory => Path.Combine(PathHelper.GetAppDataFolderPath(), "BIOS Settings", "Backups");

	public async Task BackupAsync(List<Setting> settings)
	{
		List<BackupSetting> currentSettings = [.. settings.Select(setting =>
		{
			return new BackupSetting
			{
				Path = setting.Path,
				Setting = setting.Name,
				Description = setting.Description,
				Minimum = setting.Minimum,
				Maximum = setting.Maximum,
				Increment = setting.Increment,
				Value = SettingState.GetDisplayValue(setting, setting.Value),
				Options = [.. setting.Options.Select(o => o.Label)],
				Default = setting.Default,
				Variable = setting.Variable,
				VariableGuid = HiiHelper.GetGuidString(setting.VariableGuid),
				Flags = setting.Flags,
				Attributes = HiiHelper.GetEfiVariableAttributeNames(setting.VarAttributes),
				Token = setting.Token,
				Offset = HiiHelper.ToHexString(setting.Offset),
				Width = HiiHelper.ToHexString(setting.Width)
			};
		})];

		string latest = string.Empty;
		if (Directory.Exists(BackupDirectory))
		{
			foreach (string file in Directory.EnumerateFiles(BackupDirectory, "*.json*"))
			{
				if (string.Compare(Path.GetFileName(file), Path.GetFileName(latest), StringComparison.Ordinal) > 0)
					latest = file;
			}
		}

		if (latest.Length > 0 && context.LastBackupSettings == null)
		{
			BackupFile? previous = await ReadBackupFileAsync(latest);
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

		string timestamp = $"{DateTime.Now.ToLocalTime():yyyy-MM-dd_HH-mm-ss}";
		string path = Path.Combine(BackupDirectory, $"{timestamp}.json.zip");
		await using FileStream fs = File.Create(path);
		using ZipArchive archive = new(fs, ZipArchiveMode.Create);
		ZipArchiveEntry entry = archive.CreateEntry($"{timestamp}.json", CompressionLevel.Optimal);
		await using Stream entryStream = entry.Open();
		await JsonSerializer.SerializeAsync(entryStream, backup, BackupJsonContextRelaxed.BackupFile);

		context.LastBackupSettings = currentSettings;
	}

	internal static async Task<BackupFile?> ReadBackupFileAsync(string filePath)
	{
		await using FileStream fs = File.OpenRead(filePath);
		if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			return await JsonSerializer.DeserializeAsync(fs, BackupJsonContextRelaxed.BackupFile);

		using ZipArchive archive = new(fs, ZipArchiveMode.Read);
		ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(static e => e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
		if (entry == null)
			return null;

		await using Stream entryStream = entry.Open();
		return await JsonSerializer.DeserializeAsync(entryStream, BackupJsonContextRelaxed.BackupFile);
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		BackupFile? backup = await ReadBackupFileAsync(filePath);
		if (backup == null)
			return infoService.GetWriteProtectedState();

		LastDriverError = null;

		(PageMode Result, bool Failed) = await Task.Run(() =>
		{
			using AmiSmmTransport transport = new();
			if (!transport.TryLoadAndInit())
			{
				LastDriverError = transport.LastLoadError ?? transport.LastInitError;
				return (PageMode.DriverLoadFailed, true);
			}

			Dictionary<(string Variable, Guid Guid, uint Offset), Setting> settingsByKey = [with((context.LastSettings?.Count ?? 0))];
			if (context.LastSettings != null)
			{
				foreach (Setting s in context.LastSettings)
					settingsByKey[(s.Variable, s.VariableGuid, s.Offset)] = s;
			}

			bool anyFailed = false;

			foreach (IGrouping<(string Name, Guid Guid), BackupSetting> group in backup.Settings.GroupBy(static setting => (Name: setting.Variable, Guid: Guid.TryParse(setting.VariableGuid, out Guid guid) ? guid : Guid.Empty)))
			{
				List<KeyValuePair<Setting, SettingState>> pairs = [with(group.Count())];
				foreach (BackupSetting backupSetting in group)
				{
					if (string.IsNullOrEmpty(backupSetting.Value))
						continue;

					if (!Guid.TryParse(backupSetting.VariableGuid, out Guid parsedGuid))
						continue;

					if (!HiiHelper.TryParseHexUInt32(backupSetting.Offset, out uint offset))
						continue;

					if (settingsByKey.TryGetValue((backupSetting.Variable, parsedGuid, offset), out Setting? current))
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

	private static bool SettingsEqual(List<BackupSetting> previous, List<BackupSetting> current)
	{
		if (previous.Count != current.Count)
			return false;

		Dictionary<(string Variable, string VariableGuid, uint Offset), string> previousMap = [with(previous.Count)];
		foreach (BackupSetting p in previous)
		{
			if (!HiiHelper.TryParseHexUInt32(p.Offset, out uint offset))
				return false;

			previousMap[(p.Variable, p.VariableGuid.ToUpperInvariant(), offset)] = p.Value;
		}

		foreach (BackupSetting setting in current)
		{
			if (!HiiHelper.TryParseHexUInt32(setting.Offset, out uint currentOffset))
				return false;

			if (!previousMap.TryGetValue((setting.Variable, setting.VariableGuid.ToUpperInvariant(), currentOffset), out string? prevValue) || !string.Equals(prevValue, setting.Value, StringComparison.Ordinal))
				return false;
		}

		return true;
	}
}
