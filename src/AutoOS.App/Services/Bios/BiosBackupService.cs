using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoOS.App.Data.Contexts;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;
using AutoOS.Core.Helpers.Logging;

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
				Offset = setting.Offset,
				Width = setting.Width,
				Token = setting.Token
			};
		})];

		string latest = Directory.Exists(BackupDirectory)
			? Directory.EnumerateFiles(BackupDirectory, "*.json").OrderByDescending(Path.GetFileName).FirstOrDefault() ?? string.Empty
			: string.Empty;

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
		await JsonSerializer.SerializeAsync(fs, backup, BackupJsonOptions);

		context.LastBackupSettings = currentSettings;
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		await using FileStream fs = File.OpenRead(filePath);
		BackupFile? backup = await JsonSerializer.DeserializeAsync(fs, BackupJsonContext.Default.BackupFile);
		if (backup == null)
			return infoService.GetWriteProtectedState();

		bool failed = await Task.Run(() =>
		{
			bool anyFailed = false;
			var failureDetails = new StringBuilder();

			foreach (IGrouping<(string Name, Guid Guid), BackupSetting> group in backup.Settings.GroupBy(GetVariableKey))
			{
				if (group.Key.Guid == Constants.Bios.SecureBootVarStoreGuid)
					continue;

				var pairs = new List<KeyValuePair<Setting, SettingState>>();
				foreach (BackupSetting backupSetting in group)
				{
					Setting? current = (context.LastSettings ?? [])
						.FirstOrDefault(setting =>
							string.Equals(setting.VariableName, backupSetting.VariableName, StringComparison.Ordinal) &&
							setting.VariableGuid == group.Key.Guid &&
							setting.Offset == backupSetting.Offset);

					if (current != null && !string.IsNullOrEmpty(backupSetting.Value))
						pairs.Add(new KeyValuePair<Setting, SettingState>(current, new SettingState { Value = backupSetting.Value }));
				}

				if (pairs.Count == 0)
					continue;

				if (!nvramService.PatchVariable(pairs, out byte[]? patched, out uint attributes) || patched == null)
				{
					failureDetails.AppendLine($"PatchVariable failed for '{group.Key.Name}' ({group.Key.Guid}) with {pairs.Count} settings");
					anyFailed = true;
					continue;
				}

				if (nvramService.TryGetCurrentBlob(pairs[0].Key, out byte[]? currentBlob, out _) && currentBlob != null && currentBlob.AsSpan().SequenceEqual(patched))
					continue;

				if (!HiiHelper.TrySetVariable(group.Key.Name, group.Key.Guid, patched, attributes, out int win32Error))
				{
					failureDetails.AppendLine($"TrySetVariable failed for '{group.Key.Name}' ({group.Key.Guid}), patched length {patched.Length}, attributes 0x{attributes:X}, {HiiHelper.FormatWin32Error(win32Error)}");
					anyFailed = true;
				}
			}

			if (anyFailed)
				LogHelper.LogError(new Exception(failureDetails.ToString()), actionTitle: $"Restore from backup partially failed");

			return anyFailed;
		});

		return failed ? infoService.GetWriteProtectedState() : PageMode.Loaded;
	}

	private static (string Name, Guid Guid) GetVariableKey(BackupSetting setting) =>
		(setting.VariableName, Guid.TryParse(setting.VariableGuid, out Guid guid) ? guid : Guid.Empty);

	private static bool SettingsEqual(List<BackupSetting> previous, List<BackupSetting> current)
	{
		if (previous.Count != current.Count)
			return false;

		foreach (BackupSetting setting in current)
		{
			BackupSetting? match = previous.FirstOrDefault(p =>
				string.Equals(p.VariableName, setting.VariableName, StringComparison.Ordinal) &&
				string.Equals(p.VariableGuid, setting.VariableGuid, StringComparison.OrdinalIgnoreCase) &&
				p.Offset == setting.Offset);

			if (match == null || !string.Equals(match.Value, setting.Value, StringComparison.Ordinal))
				return false;
		}

		return true;
	}
}
