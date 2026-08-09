using System.Diagnostics;
using System.Text.RegularExpressions;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.App.Data.Models.Bios;
using AutoOS.App.Views.Installer.Stages;
using AutoOS.Core.Helpers.Logging;
using DevWinUI;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace AutoOS.App.Services.Bios;

public class BiosSettingsService : IBiosSettingsService
{
	private static readonly string[] ProtectedChipsets = ["Z790", "B760", "H770", "X870", "X670", "B650", "A620"];

	private readonly string scewinDirectory = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN");
	private readonly string nvramPath = Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt");
	private readonly string assetsScewinExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN", "SCEWIN_64.exe");

	public string BackupDirectory => Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "Backups");

	public async Task<(PageMode State, IReadOnlyList<Setting> Settings)> LoadAsync()
	{
		if (!Directory.Exists(scewinDirectory))
			FileSystem.CopyDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "SCEWIN"), scewinDirectory, true);

		Process.GetProcessesByName("SCEWIN_64").FirstOrDefault()?.Kill();

		(string manufacturer, string product) = GetBoardInfo();

		(string _, string errorOutput) = await RunScewinAsync($"/o /s \"{nvramPath}\"");

		if (errorOutput.Contains("AMISCE is not supported on this system.", StringComparison.OrdinalIgnoreCase) ||
			errorOutput.Contains("BIOS not compatible", StringComparison.OrdinalIgnoreCase))
		{
			return (PageMode.Unsupported, []);
		}

		if (errorOutput.Contains("WARNING: HII data does not have setup questions information", StringComparison.OrdinalIgnoreCase))
			return (GetHiiState(manufacturer, product), []);

		if (errorOutput.Contains("Platform identification failed.", StringComparison.OrdinalIgnoreCase))
			(_, errorOutput) = await RunScewinAsync($"/o /s \"{nvramPath}\" /d");

		if (!errorOutput.Contains("Script file exported successfully.", StringComparison.OrdinalIgnoreCase))
			return (PageMode.Exporting, []);

		if (new FileInfo(nvramPath).Length <= 100 * 1024)
			return (GetHiiState(manufacturer, product), []);

		await CreateBackupIfNeededAsync();

		List<Setting> settings = await Task.Run(ParseSettings);
		return (PageMode.Loaded, settings);
	}

	public async Task<PageMode> ImportToNvramAsync(IEnumerable<KeyValuePair<Setting, State>> modifiedSettings)
	{
		List<KeyValuePair<Setting, State>> modified = modifiedSettings.ToList();
		if (modified.Count == 0)
			return PageMode.Loaded;

		List<string> lines = modified[0].Key.OriginalLines!.ToList();

		foreach ((Setting setting, State state) in modified)
		{
			if (setting.HasValueField)
				UpdateValue(setting, state, lines);
			else if (setting.HasOptions)
				UpdateOption(setting, state, lines);
		}

		await File.WriteAllLinesAsync(nvramPath, lines);

		(string _, string errorOutput) = await RunScewinAsync($"/i /s \"{nvramPath}\"", assetsScewinExe);
		(string manufacturer, _) = GetBoardInfo();

		if ((errorOutput.Contains("WARNING : Cannot update protected variable", StringComparison.OrdinalIgnoreCase) ||
			 errorOutput.Contains("WARNING : Error in writing variable", StringComparison.OrdinalIgnoreCase)) &&
			!errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase))
		{
			return GetWriteProtectedState(manufacturer);
		}

		return PageMode.Loaded;
	}

	public async Task<PageMode> RestoreFromBackupAsync(string filePath)
	{
		(string _, string errorOutput) = await RunScewinAsync($"/i /s \"{filePath}\"", assetsScewinExe);
		(string manufacturer, _) = GetBoardInfo();

		if (errorOutput.Contains("Warning: Error in writing variable", StringComparison.OrdinalIgnoreCase))
			return GetWriteProtectedState(manufacturer);

		if (errorOutput.Contains("Script file imported successfully.", StringComparison.OrdinalIgnoreCase) ||
			errorOutput.Contains("System configuration not modified.", StringComparison.OrdinalIgnoreCase))
		{
			return PageMode.Loaded;
		}

		return PageMode.Loaded;
	}

	private async Task<(string Output, string ErrorOutput)> RunScewinAsync(string arguments, string? fileName = null)
	{
		using var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = fileName ?? Path.Combine(scewinDirectory, "SCEWIN_64.exe"),
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true
			}
		};

		process.Start();
		string errorOutput = await process.StandardError.ReadToEndAsync();
		string output = await process.StandardOutput.ReadToEndAsync();
		await process.WaitForExitAsync();
		return (output, errorOutput);
	}

	private static (string Manufacturer, string Product) GetBoardInfo()
	{
		string manufacturer = "Unknown";
		string product = "Unknown";

		using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
		if (key != null)
		{
			manufacturer = key.GetValue("BaseBoardManufacturer")?.ToString()?.ToLowerInvariant() ?? "Unknown";
			product = key.GetValue("BaseBoardProduct")?.ToString()?.ToUpperInvariant() ?? "Unknown";
		}

		return (manufacturer, product);
	}

	private static PageMode GetHiiState(string manufacturer, string product)
	{
		if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
		{
			return ProtectedChipsets.Any(chipset => product.Contains(chipset))
				? PageMode.HiiResourcesProtected
				: PageMode.HiiResourcesRegular;
		}

		return PageMode.HiiResourcesOther;
	}

	private static PageMode GetWriteProtectedState(string manufacturer)
	{
		if (manufacturer.Contains("asus") || manufacturer.Contains("asustek"))
			return PageMode.WriteProtectedAsus;
		if (manufacturer.Contains("asrock"))
			return PageMode.WriteProtectedAsRock;
		return PageMode.WriteProtectedOther;
	}

	private async Task CreateBackupIfNeededAsync()
	{
		if (!Directory.Exists(BackupDirectory))
			Directory.CreateDirectory(BackupDirectory);

		string[] currentLines = await File.ReadAllLinesAsync(nvramPath);

		var existingBackups = Directory.GetFiles(BackupDirectory, "*.txt").OrderByDescending(file => Path.GetFileName(file)).ToList();

		bool needsBackup = true;

		if (existingBackups.Count > 0)
		{
			string[] lastBackupLines = await File.ReadAllLinesAsync(existingBackups[0]);
			var currentSettings = BiosSettingsParser.ParseFromLines(currentLines).ToList();
			var backupSettings = BiosSettingsParser.ParseFromLines(lastBackupLines).ToList();
			needsBackup = !SettingsEqual(currentSettings, backupSettings);
		}
		else
		{
			try
			{
				_ = LogHelper.Log(PreparingStage.GPUs, true);
			}
			catch (Exception ex)
			{
				await LogHelper.LogFallbackError(ex);
			}
		}

		if (needsBackup)
			await File.WriteAllLinesAsync(Path.Combine(BackupDirectory, $"{DateTime.Now.ToLocalTime():yyyy-MM-dd_HH-mm-ss}.txt"), currentLines);
	}

	private static bool SettingsEqual(List<Setting> current, List<Setting> backup)
	{
		if (current.Count != backup.Count)
			return false;

		for (int i = 0; i < current.Count; i++)
		{
			Setting currentSetting = current[i];
			Setting backupSetting = backup[i];

			if (currentSetting.SetupQuestion != backupSetting.SetupQuestion || currentSetting.Value != backupSetting.Value || currentSetting.Options.Count != backupSetting.Options.Count)
				return false;

			for (int j = 0; j < currentSetting.Options.Count; j++)
			{
				if (currentSetting.Options[j].Label != backupSetting.Options[j].Label)
					return false;
			}

			if (currentSetting.SelectedOption?.Index != backupSetting.SelectedOption?.Index)
				return false;
		}

		return true;
	}

	private static List<Setting> ParseSettings()
	{
		List<Setting> settings;

		using FileStream stream = File.OpenRead(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt"));
		settings = [.. BiosSettingsParser.ParseFromStream(stream).Settings];

		foreach (Setting setting in settings)
		{
			if (setting.HasValueField)
				setting.OriginalValue = setting.Value;

			if (setting.HasOptions)
				setting.OriginalSelectedOption = setting.SelectedOption;

			var matchingRules = Recommendations.Rules
				.Where(rule => string.Equals(rule.SetupQuestion?.Trim(), setting.SetupQuestion?.Trim(), StringComparison.OrdinalIgnoreCase))
				.Where(rule => rule.Condition == null || rule.Condition(settings))
				.OrderByDescending(rule => rule.Condition != null)
				.ToList();

			foreach (Recommendation? rule in matchingRules)
			{
				string? recommendedLabel = rule.RecommendedOption?.Trim().ToLowerInvariant();
				bool ruleApplicable = false;

				if ((rule.Type?.Equals("Option", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasOptions)
				{
					Option? recommended = setting.Options
						.FirstOrDefault(option => option.Label?.Trim().ToLowerInvariant() == recommendedLabel);

					if (recommended != null)
					{
						ruleApplicable = true;
						setting.RecommendedOption = recommended;

						if (setting.SelectedOption?.Label?.Trim().ToLowerInvariant() != recommended.Label?.ToLowerInvariant())
							setting.IsRecommended = true;
					}
				}

				if ((rule.Type?.Equals("Value", StringComparison.OrdinalIgnoreCase) ?? false) && setting.HasValueField)
				{
					ruleApplicable = true;
					string? currentValue = setting.Value?.Trim().ToLowerInvariant();
					setting.RecommendedValue = rule.RecommendedOption;

					if (!string.IsNullOrEmpty(currentValue) && currentValue != recommendedLabel)
						setting.IsRecommended = true;
				}

				if (ruleApplicable)
					break;
			}
		}

		return settings;
	}

	private static void UpdateValue(Setting setting, State state, List<string>? lines)
	{
		if (lines == null)
			return;
		if (setting.Line < 0 || setting.Line >= lines.Count)
			return;

		int valueLineIndex = -1;

		for (int i = setting.Line; i < lines.Count; i++)
		{
			if (lines[i].TrimStart().StartsWith("Value", StringComparison.OrdinalIgnoreCase))
			{
				valueLineIndex = i;
				break;
			}
		}

		if (valueLineIndex == -1)
			return;

		string line = lines[valueLineIndex];

		int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
		string valuePart = commentIndex >= 0 ? line[..commentIndex] : line;
		string commentPart = commentIndex >= 0 ? line[commentIndex..] : "";

		int equalsIndex = valuePart.IndexOf('=');
		if (equalsIndex < 0)
			return;

		string prefix = valuePart.Substring(0, equalsIndex + 1);
		string originalValueText = valuePart.Substring(equalsIndex + 1);

		char firstChar = originalValueText.TrimStart().FirstOrDefault();
		char lastChar = originalValueText.TrimEnd().LastOrDefault();
		string innerValue = state.Value ?? "";

		if ((firstChar == '<' && lastChar == '>') ||
			(firstChar == '"' && lastChar == '"') ||
			(firstChar == '{' && lastChar == '}'))
		{
			int leadingSpaces = originalValueText.TakeWhile(char.IsWhiteSpace).Count();
			int trailingSpaces = originalValueText.Reverse().TakeWhile(char.IsWhiteSpace).Count();
			string leading = new(' ', leadingSpaces);
			string trailing = new(' ', trailingSpaces);

			lines[valueLineIndex] = $"{prefix}{leading}{firstChar}{innerValue}{lastChar}{trailing}{commentPart}";
		}
		else
		{
			lines[valueLineIndex] = $"{prefix}{originalValueText.Replace(originalValueText.Trim(), innerValue)}{commentPart}";
		}
	}

	private static void UpdateOption(Setting setting, State state, List<string>? lines)
	{
		if (lines == null)
			return;
		if (setting.Line < 0 || setting.Line >= lines.Count)
			return;

		int optionsIdx = -1;
		for (int i = setting.Line; i < lines.Count; i++)
			if (lines[i].TrimStart().StartsWith("Options", StringComparison.OrdinalIgnoreCase))
			{
				optionsIdx = i;
				break;
			}

		if (optionsIdx == -1)
			return;

		string optLine = lines[optionsIdx];
		int cIdx = optLine.IndexOf("//");
		string comment = "";
		string optionsPart = optLine;
		if (cIdx >= 0)
		{
			int startComment = cIdx;
			while (startComment > 0 && char.IsWhiteSpace(optLine[startComment - 1])) startComment--;
			comment = optLine[startComment..];
			optionsPart = optLine[..startComment];
		}

		int eq = optionsPart.IndexOf('=');
		if (eq < 0) return;
		string prefix = optionsPart[..(eq + 1)];
		string optionsText = optionsPart[(eq + 1)..];

		MatchCollection matches = Regex.Matches(optionsText, @"(\*?\[\w+\][^\[\]\n\r\t\f\v]*)");
		var newParts = new List<string>(matches.Count);

		foreach (Match m in matches)
		{
			string opt = m.Value;
			Match idm = Regex.Match(opt, @"\*?\[(\w+)\]");
			string? idx = idm.Success ? idm.Groups[1].Value : null;
			string withoutStar = opt.TrimStart('*');

			if (state.SelectedOption == null)
			{
				newParts.Add(opt);
				continue;
			}

			if (idx == state.SelectedOption.Index)
			{
				if (!opt.StartsWith('*'))
					opt = "*" + withoutStar;
			}
			else if (opt.StartsWith('*'))
			{
				opt = withoutStar;
			}

			newParts.Add(opt);
		}

		lines[optionsIdx] = prefix + string.Join(" ", newParts) + comment;

		int ptr = optionsIdx + 1;
		while (ptr < lines.Count)
		{
			string original = lines[ptr];
			string trimmed = original.TrimStart();

			if (trimmed.StartsWith('[') || trimmed.StartsWith("*["))
			{
				Match idxM = Regex.Match(trimmed, @"^\*?\[(\w+)\]");
				string? idx = idxM.Success ? idxM.Groups[1].Value : null;
				string indent = original[..(original.Length - trimmed.Length)];
				string withoutStar = trimmed.StartsWith('*') ? trimmed[1..] : trimmed;

				lines[ptr] = (state.SelectedOption != null && idx == state.SelectedOption.Index)
					? indent + "*" + withoutStar
					: indent + withoutStar;
				ptr++;
				continue;
			}

			break;
		}
	}
}
