using System.Text.RegularExpressions;
using AutoOS.App.Data.Models.Bios;

namespace AutoOS.App.Services.Bios;

public partial class BiosSettingsParser
{
	public static IEnumerable<BiosSettingsModel> ParseFromStream(Stream stream)
	{
		var lines = new List<string>();
		using (var reader = new StreamReader(stream))
		{
			string? line;
			while ((line = reader.ReadLine()) != null)
			{
				lines.Add(line);
			}
		}

		return ParseFromLines(lines);
	}

	public static IEnumerable<BiosSettingsModel> ParseFromLines(IEnumerable<string> lines)
	{
		BiosSettingsModel? current = null;
		bool readingOptions = false;
		var lineList = lines.ToList();

		for (int i = 0; i < lineList.Count; i++)
		{
			string line = lineList[i].Trim();
			if (string.IsNullOrWhiteSpace(line)) continue;

			if (line.StartsWith("Setup Question", StringComparison.OrdinalIgnoreCase))
			{
				if (current != null)
				{
					yield return current;
				}

				current = new BiosSettingsModel
				{
					Line = i,
					OriginalLines = lineList
				};
				readingOptions = false;

				string[] parts = line.Split('=', 2);
				if (parts.Length == 2)
				{
					string rawQuestion = parts[1].Trim().Replace('\uFFFD', '™');

					Match match = TrailingWordRegex().Match(rawQuestion);
					current.SetupQuestion = match.Success ? match.Groups[1].Value : rawQuestion;
				}

				continue;
			}

			if (current == null) continue;

			if (line.StartsWith("Help String", StringComparison.OrdinalIgnoreCase))
			{
				current.HelpString = line.Split('=', 2)[1].Trim().Replace('\uFFFD', '°');
				continue;
			}

			if (line.StartsWith("Token", StringComparison.OrdinalIgnoreCase))
			{
				current.Token = line.Split('=', 2)[1].Split("//")[0].Trim();
				continue;
			}

			if (line.StartsWith("Offset", StringComparison.OrdinalIgnoreCase))
			{
				current.Offset = line.Split('=', 2)[1].Trim();
				continue;
			}

			if (line.StartsWith("Width", StringComparison.OrdinalIgnoreCase))
			{
				current.Width = line.Split('=', 2)[1].Trim();
				continue;
			}

			if (line.StartsWith("BIOS Default", StringComparison.OrdinalIgnoreCase))
			{
				string part = line.Split('=', 2)[1].Split("//")[0].Trim();
				Match match = Regex.Match(part, @"\[[^\]]+\](.+)");
				current.BiosDefault = match.Success ? match.Groups[1].Value.Trim() : ExtractValue(part);
				continue;
			}

			if (line.StartsWith("Value", StringComparison.OrdinalIgnoreCase))
			{
				string valuePart = line.Split('=', 2)[1].Split("//")[0].Trim();
				current.Value = ExtractValue(valuePart);
				continue;
			}

			if (line.StartsWith("Options", StringComparison.OrdinalIgnoreCase))
			{
				current.Options = [];
				readingOptions = true;

				string inline = line[(line.IndexOf('=') + 1)..].Trim();
				if (!string.IsNullOrWhiteSpace(inline))
					ParseOptionLine(inline, current.Options);

				continue;
			}

			if (readingOptions)
			{
				if (line.StartsWith("[") || line.StartsWith("*["))
				{
					ParseOptionLine(line, current.Options);
				}
				else
				{
					readingOptions = false;
				}
			}
		}

		if (current != null)
		{
			yield return current;
		}

		static string ExtractValue(string raw)
		{
			raw = raw.Trim();

			if (raw.StartsWith("\"") && raw.EndsWith("\""))
				return raw[1..^1];
			if (raw.StartsWith("<") && raw.EndsWith(">"))
				return raw[1..^1];
			if (raw.StartsWith("{") && raw.EndsWith("}"))
				return raw.TrimStart('{').TrimEnd('}').Trim();

			return raw;
		}

		static void ParseOptionLine(string line, List<Option> options)
		{
			Match match = Regex.Match(line, @"^\*?\[(\w+)\](.*)$");
			if (!match.Success) return;

			bool isSelected = line.StartsWith("*");
			string index = match.Groups[1].Value.Trim();
			string label = match.Groups[2].Value.Trim();
			int commentIndex = label.IndexOf("//");
			if (commentIndex >= 0) label = label[..commentIndex].Trim();

			options.Add(new Option
			{
				Index = index,
				Label = label,
				IsSelected = isSelected
			});
		}
	}

	[GeneratedRegex(@"^(.*?)\s{2,}\w+$", RegexOptions.Compiled)]
	private static partial Regex TrailingWordRegex();
}
