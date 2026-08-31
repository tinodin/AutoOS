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
	public string? LastDriverError { get; private set; }

	private void LogDriverError()
	{
		string error = LastDriverError ?? "Unknown SMM driver failure";
		LogHelper.LogError(new Exception(error), actionTitle: "SMM driver load failed");
	}

	public async Task<(PageMode Result, IReadOnlyList<Setting> Settings)> ReadFromNvramAsync()
	{
		LastDriverError = null;

		(PageMode Result, List<Setting> Settings, Dictionary<ushort, QidTarget> QidMap) = await Task.Run(() =>
		{
			if (!AmiPlatformHelper.IsSupported())
				return (PageMode.Unsupported, new List<Setting>(), new Dictionary<ushort, QidTarget>());

			using AmiSmmTransport transport = new();
			if (!transport.TryLoad())
			{
				LastDriverError = transport.LastLoadError;
				return (PageMode.DriverLoadFailed, new List<Setting>(), new Dictionary<ushort, QidTarget>());
			}

			if (!transport.TryInitSmm())
			{
				LastDriverError = transport.LastInitError ?? transport.LastLoadError;
				return (PageMode.DriverLoadFailed, new List<Setting>(), new Dictionary<ushort, QidTarget>());
			}

			if (!HiiHelper.TryReadHiiDb(transport, out byte[]? database) || database == null)
				return (PageMode.Unsupported, new List<Setting>(), new Dictionary<ushort, QidTarget>());

			string language = HiiHelper.TryGetBiosLanguage(transport, out string biosLanguage) ? biosLanguage : "en-US";
			List<Setting> settings = HiiHelper.ParseDatabase(database, language, out Dictionary<ushort, QidTarget> qidMap);
			if (settings.Count == 0)
				return (infoService.GetHiiState(), new List<Setting>(), qidMap);

			nvramService.LoadCurrentValues(settings, qidMap, transport);
			Recommendations.GetRecommendations(settings);

			return (PageMode.Loaded, settings, qidMap);
		});

		if (Result == PageMode.DriverLoadFailed)
			LogDriverError();

		if (Result != PageMode.Loaded)
			return (Result, Array.Empty<Setting>());

		context.LastSettings = Settings;
		context.LastQidMap = QidMap;

		return (PageMode.Loaded, Settings);
	}

	public async Task<(PageMode Result, IReadOnlyList<Setting> FailedSettings)> WriteToNvramAsync(IEnumerable<KeyValuePair<Setting, SettingState>> modifiedSettings)
	{
		LastDriverError = null;

		List<KeyValuePair<Setting, SettingState>> modified = [.. modifiedSettings];
		if (modified.Count == 0)
			return (PageMode.Loaded, Array.Empty<Setting>());

		using AmiSmmTransport transport = new();
		if (!transport.TryLoad())
		{
			LastDriverError = transport.LastLoadError;
			LogDriverError();
			return (PageMode.DriverLoadFailed, modified.Select(p => p.Key).ToList());
		}

		if (!transport.TryInitSmm())
		{
			LastDriverError = transport.LastInitError ?? transport.LastLoadError;
			LogDriverError();
			return (PageMode.DriverLoadFailed, modified.Select(p => p.Key).ToList());
		}

		(PageMode state, List<Setting> failed) = await Task.Run(() =>
		{
			List<Setting> failures = [];

			foreach (IGrouping<(string Name, Guid Guid), KeyValuePair<Setting, SettingState>> group in modified.GroupBy(pair => (pair.Key.VariableName, pair.Key.VariableGuid)))
			{
				if (!nvramService.PatchVariable(group, out byte[]? patched, out uint attributes, transport))
				{
					failures.AddRange(group.Select(pair => pair.Key));
					continue;
				}

				if (patched == null)
				{
					failures.AddRange(group.Select(pair => pair.Key));
					continue;
				}

				if (!transport.TrySetVariable(group.Key.Name, group.Key.Guid, attributes, patched, out uint _))
					failures.AddRange(group.Select(pair => pair.Key));
			}

			if (failures.Count > 0)
				return (infoService.GetWriteProtectedState(), failures);

			if (context.LastSettings != null && context.LastQidMap != null)
				nvramService.LoadCurrentValues(context.LastSettings, context.LastQidMap, transport);

			return (PageMode.Loaded, failures);
		});

		if (state == PageMode.Loaded && failed.Count == 0 && context.LastSettings != null && context.LastQidMap != null)
			await backupService.BackupAsync(context.LastSettings);

		return (state, failed);
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		LastDriverError = null;
		PageMode result = await backupService.RestoreFromBackupAsync(filePath);
		if (result == PageMode.DriverLoadFailed)
		{
			LastDriverError = backupService.LastDriverError;
			LogDriverError();
		}

		return result;
	}
}
