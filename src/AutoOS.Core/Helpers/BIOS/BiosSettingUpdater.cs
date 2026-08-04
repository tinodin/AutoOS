using System.Text.RegularExpressions;
using AutoOS.Core.Helpers.Logging;
using DevWinUI;

namespace AutoOS.Core.Helpers.BIOS;

public static class BiosSettingUpdater
{
	public static void SaveSingleSetting(BiosSettingModel setting)
	{
		// get lines from nvram
		List<string> lines = setting.OriginalLines;

		// update settings
		if (setting.HasValueField)
		{
			UpdateValue(setting, lines);
		}
		else if (setting.HasOptions)
		{
			UpdateOption(setting, lines);
		}

		// write changes
		File.WriteAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt"), lines);
	}

	public static void SaveAllSettings(IEnumerable<BiosSettingModel> modifiedSettings)
	{
		// get lines from nvram
		List<string> lines = modifiedSettings.First().OriginalLines;

		// update settings
		foreach (BiosSettingModel setting in modifiedSettings)
		{
			if (setting.HasValueField)
			{
				UpdateValue(setting, lines);
			}
			else if (setting.HasOptions)
			{
				UpdateOption(setting, lines);
			}
		}

		// write changes
		File.WriteAllLines(Path.Combine(PathHelper.GetAppDataFolderPath(), "SCEWIN", "nvram.txt"), lines);
	}

	public static void UpdateValue(BiosSettingModel setting, List<string> lines = null)
	{
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
		string innerValue = setting.Value ?? "";

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

	public static void UpdateOption(BiosSettingModel setting, List<string> lines = null)
	{
		if (setting.Line < 0 || setting.Line >= lines.Count)
			return;

		int optionsIdx = -1;
		for (int i = setting.Line; i < lines.Count; i++)
			if (lines[i].TrimStart().StartsWith("Options", StringComparison.OrdinalIgnoreCase))
			{
				optionsIdx = i;
				break;
			}

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
			string idx = idm.Success ? idm.Groups[1].Value : null;
			string withoutStar = opt.TrimStart('*');

			if (setting.SelectedOption == null)
			{
				newParts.Add(opt);
				continue;
			}

			if (idx == setting.SelectedOption.Index)
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
				string idx = idxM.Success ? idxM.Groups[1].Value : null;
				string indent = original[..(original.Length - trimmed.Length)];
				string withoutStar = trimmed.StartsWith('*') ? trimmed[1..] : trimmed;

				lines[ptr] = (setting.SelectedOption != null && idx == setting.SelectedOption.Index)
				? indent + "*" + withoutStar
				: indent + withoutStar;
				ptr++;
				continue;
			}

			break;
		}
	}
}
