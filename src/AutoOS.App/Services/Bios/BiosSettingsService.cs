using System.Text;
using AutoOS.App.Data.Contexts;
using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;
using AutoOS.Core.Helpers.Logging;

namespace AutoOS.App.Services.Bios;

public sealed class BiosSettingsService(IBiosSettingsContext context, IBiosNvramService nvramService, IBiosBackupService backupService, IBiosInfoService infoService) : IBiosSettingsService
{
	public async Task<(PageMode Result, IReadOnlyList<Setting> Settings)> ReadFromNvramAsync()
	{
		if (!HiiHelper.TryReadHiiDb(out byte[]? database) || database == null)
			return (PageMode.Unsupported, Array.Empty<Setting>());

		Dictionary<ushort, QidTarget> qidMap = null!;
		string language = HiiHelper.TryGetBiosLanguage(out string biosLanguage) ? biosLanguage : "en-US";
		List<Setting> settings = await Task.Run(() => HiiHelper.ParseDatabase(database, language, out qidMap));
		if (settings.Count == 0)
			return (infoService.GetHiiState(), Array.Empty<Setting>());

		context.LastSettings = settings;
		context.LastQidMap = qidMap;
		nvramService.LoadCurrentValues(settings, qidMap);
		Recommendations.GetRecommendations(settings);
		await backupService.BackupAsync(settings);

		return (PageMode.Loaded, settings);
	}

	public async Task<(PageMode Result, IReadOnlyList<Setting> FailedSettings)> WriteToNvramAsync(IEnumerable<KeyValuePair<Setting, SettingState>> modifiedSettings)
	{
		var modified = modifiedSettings.ToList();
		if (modified.Count == 0)
			return (PageMode.Loaded, Array.Empty<Setting>());

		(PageMode state, List<Setting> failed) = await Task.Run(() =>
		{
			var failures = new List<Setting>();
			var failureDetails = new StringBuilder();

			foreach (IGrouping<(string Name, Guid Guid), KeyValuePair<Setting, SettingState>> group in modified.GroupBy(pair => (pair.Key.VariableName, pair.Key.VariableGuid)))
			{
				if (group.Key.Guid == Constants.Bios.SecureBootVarStoreGuid)
					continue;

				if (!nvramService.PatchVariable(group, out byte[]? patched, out uint attributes) || patched == null)
				{
					failureDetails.AppendLine($"PatchVariable failed for '{group.Key.Name}' ({group.Key.Guid})");
					failures.AddRange(group.Select(pair => pair.Key));
					continue;
				}

				if (nvramService.TryGetCurrentBlob(group.First().Key, out byte[]? currentBlob, out _) && currentBlob != null && currentBlob.AsSpan().SequenceEqual(patched))
					continue;

				if (!HiiHelper.TrySetVariable(group.Key.Name, group.Key.Guid, patched, attributes, out int win32Error))
				{
					failureDetails.AppendLine($"TrySetVariable failed for '{group.Key.Name}' ({group.Key.Guid}), patched length {patched.Length}, attributes 0x{attributes:X}, {HiiHelper.FormatWin32Error(win32Error)}");
					failures.AddRange(group.Select(pair => pair.Key));
				}
			}

			if (failures.Count > 0)
			{
				failureDetails.AppendLine($"Failed {failures.Count} of {modified.Count} settings");
				LogHelper.LogError(new Exception(failureDetails.ToString()), actionTitle: $"Write to NVRAM partially failed");
				return (failures.Count == modified.Count ? infoService.GetWriteProtectedState() : PageMode.Loaded, failures);
			}

			if (context.LastSettings != null && context.LastQidMap != null)
				nvramService.LoadCurrentValues(context.LastSettings, context.LastQidMap);

			return (PageMode.Loaded, failures);
		});

		if (state == PageMode.Loaded && failed.Count == 0 && context.LastSettings != null && context.LastQidMap != null)
			await backupService.BackupAsync(context.LastSettings);

		return (state, failed);
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		return await backupService.RestoreFromBackupAsync(filePath);
	}
}
