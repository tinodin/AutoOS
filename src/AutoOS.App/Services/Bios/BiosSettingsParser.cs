using System.Text;
using AutoOS.App.Data.Models.Bios;

namespace AutoOS.App.Services.Bios;

public static class BiosSettingsParser
{
	public static BiosFile Parse(byte[] bytes) => ParseCore(DecodeLines(bytes));

	public static BiosFile ParseFromStream(Stream stream)
	{
		using var memory = new MemoryStream();
		stream.CopyTo(memory);
		return Parse(memory.ToArray());
	}

	public static List<Setting> ParseFromLines(IReadOnlyList<string> lines) => [.. ParseCore(lines).Settings];

	private static List<string> DecodeLines(byte[] bytes)
	{
		var lines = new List<string>(4096);
		var line = new StringBuilder(128);

		for (int i = 0; i < bytes.Length;)
		{
			byte b = bytes[i];

			if (b == 0x0A)
			{
				AddLine(lines, line);
				i++;
				continue;
			}

			if (b < 0x80)
			{
				line.Append((char)b);
				i++;
				continue;
			}

			if (b == 0xEF && i + 2 < bytes.Length && bytes[i + 1] == 0xBF && bytes[i + 2] == 0xBD)
			{
				line.Append('°');
				i += 3;
				continue;
			}

			line.Append(b switch
			{
				0xB0 or 0xF8 => '°',
				0xAE => '®',
				0xB5 => 'µ',
				0xA0 => ' ',
				_ => '\uFFFD'
			});
			i++;
		}

		AddLine(lines, line);
		return lines;
	}

	private static void AddLine(List<string> lines, StringBuilder line)
	{
		if (line.Length > 0 && line[^1] == '\r')
			line.Length--;
		lines.Add(line.ToString());
		line.Clear();
	}

	private static BiosFile ParseCore(IReadOnlyList<string> lines)
	{
		var settings = new List<Setting>();
		Setting? current = null;
		bool readingOptions = false;

		for (int i = 0; i < lines.Count; i++)
		{
			string line = lines[i];

			int skip = 0;
			while (skip < line.Length && char.IsWhiteSpace(line[skip]))
				skip++;

			if (skip == line.Length)
				continue;

			if (line[skip] == '/')
				continue;

			if (TryField(line, skip, "Setup Question", out string value))
			{
				if (current != null)
				{
					current.BlockEnd = i;
					settings.Add(current);
				}

				current = new Setting
				{
					Line = i,
					BlockStart = i,
					OriginalLines = lines
				};
				readingOptions = false;
				current.SetupQuestion = CleanSetupQuestion(value);
				continue;
			}

			if (current == null)
				continue;

			if (readingOptions && (line[skip] == '[' || (line[skip] == '*' && skip + 1 < line.Length && line[skip + 1] == '[')))
			{
				current.OptionLineIndexes.Add(i);
				int selected = ParseOptionsInto(line, current.Options);
				if (selected >= 0)
					current.SelectedOption = current.Options[selected];
				continue;
			}

			readingOptions = false;

			if (TryField(line, skip, "Help String", out value))
			{
				current.HelpString = value.Trim();
				continue;
			}

			if (TryField(line, skip, "Token", out value))
			{
				current.Token = StripComment(value).Trim();
				continue;
			}

			if (TryField(line, skip, "Offset", out value))
			{
				current.Offset = StripComment(value).Trim();
				continue;
			}

			if (TryField(line, skip, "Width", out value))
			{
				current.Width = StripComment(value).Trim();
				continue;
			}

			if (TryField(line, skip, "BIOS Default", out value))
			{
				current.BiosDefault = ParseDefault(value);
				continue;
			}

			if (TryField(line, skip, "Value", out value))
			{
				current.ValueLineIndex = i;
				current.Value = ExtractValue(value);
				continue;
			}

			if (TryField(line, skip, "Options", out value))
			{
				readingOptions = true;
				current.Options = [];
				current.OptionLineIndexes = [];
				int selected = ParseOptionsInto(value, current.Options);
				if (current.Options.Count > 0)
					current.OptionLineIndexes.Add(i);
				if (selected >= 0)
					current.SelectedOption = current.Options[selected];
			}
		}

		if (current != null)
		{
			current.BlockEnd = lines.Count;
			settings.Add(current);
		}

		return new BiosFile
		{
			Lines = lines,
			Settings = settings,
			HeaderEnd = settings.Count > 0 ? settings[0].BlockStart : lines.Count
		};
	}

	private static bool TryField(string line, int start, string name, out string value)
	{
		if (start + name.Length > line.Length || !line.AsSpan(start, name.Length).Equals(name.AsSpan(), StringComparison.Ordinal))
		{
			value = string.Empty;
			return false;
		}

		int p = start + name.Length;
		while (p < line.Length && (line[p] == '\t' || line[p] == ' '))
			p++;

		if (p >= line.Length || line[p] != '=')
		{
			value = string.Empty;
			return false;
		}

		value = line[(p + 1)..];
		return true;
	}

	private static string CleanSetupQuestion(string raw)
	{
		string value = raw.Trim();
		if (value.Length == 0)
			return value;

		value = StripPaddedValues(value);

		value = StripSingleSpaceValues(value);

		return CollapseWhitespace(value);
	}

	private static string CollapseWhitespace(string value)
	{
		var builder = new StringBuilder(value.Length);
		bool lastWasSpace = false;
		foreach (char c in value)
		{
			if (char.IsWhiteSpace(c))
			{
				if (lastWasSpace)
					continue;
				builder.Append(' ');
				lastWasSpace = true;
			}
			else
			{
				builder.Append(c);
				lastWasSpace = false;
			}
		}
		return builder.ToString().Trim();
	}

	// Removes appended value tokens that follow the last run of 2+ whitespace.
	// Gigabyte and other exporters pad the name column, then append the current
	// value after the padding (e.g. "CAS Latency  15  15", "tRFC  312 312").
	// Everything after the last 2+ whitespace run is value junk unless it is part
	// of the name (such as "(CH A/B)" or unit suffixes like "(us)").
	private static string StripPaddedValues(string value)
	{
		while (true)
		{
			int lastRunStart = -1;

			for (int i = 0; i + 1 < value.Length; i++)
			{
				if (char.IsWhiteSpace(value[i]) && char.IsWhiteSpace(value[i + 1]))
					lastRunStart = i;
			}

			if (lastRunStart < 0)
				return value;

			int tailStart = lastRunStart + 2;
			string prefix = value[..tailStart];
			string tail = value[tailStart..];

			string stripped = StripTrailingValueTokens(tail);
			if (stripped.Length == tail.Length)
				return value;

			value = (prefix + stripped).TrimEnd();
		}
	}

	// Removes trailing value tokens from a whitespace-separated tail, keeping any
	// trailing token that belongs to the name (paren groups, unit suffixes like
	// "(us)", or index words like "Ratio Limit"). "tRFC  312 312" drops both 312s;
	// "Voltage (CH A/B)" keeps "(CH A/B)".
	private static string StripTrailingValueTokens(string tail)
	{
		string[] tokens = tail.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		int keep = tokens.Length;

		for (int i = tokens.Length - 1; i >= 0; i--)
		{
			if (IsValueToken(tokens[i]))
				keep = i;
			else
				break;
		}

		if (keep == tokens.Length)
			return tail;

		return tokens.Length == keep + 1 && !tail.Contains(" ") ? string.Empty : string.Join(' ', tokens.Take(keep));
	}

	// Removes appended value tokens separated by single spaces. A trailing run of
	// 3+ numeric tokens is always value junk ("CAS Latency 15 15 15"); a run of
	// exactly 2 drops only the last token so a name-index survives ("Turbo E-Core
	// Ratio 1 43"); a single trailing numeric is only dropped when it follows a
	// closing paren, which marks the start of a value ("Turbo Ratio (4 P-Core
	// Active) 41"). Names whose number is part of the title are left alone
	// ("Sata Port 6").
	private static string StripSingleSpaceValues(string value)
	{
		int lastRun = -1;
		for (int i = 0; i + 1 < value.Length; i++)
		{
			if (char.IsWhiteSpace(value[i]) && char.IsWhiteSpace(value[i + 1]))
				lastRun = i;
		}

		string prefix = lastRun < 0 ? string.Empty : value[..(lastRun + 1)];
		string tail = lastRun < 0 ? value : value[(lastRun + 1)..];
		tail = tail.Trim();

		string[] tokens = tail.Split(' ');
		int count = 0;
		for (int i = tokens.Length - 1; i >= 0; i--)
		{
			if (IsNumericToken(tokens[i]))
				count++;
			else
				break;
		}

		if (count == 0)
			return value;

		if (count >= 3)
			return (prefix + string.Join(' ', tokens[..(tokens.Length - count)])).TrimEnd();

		if (count == 2)
			return (prefix + string.Join(' ', tokens[..(tokens.Length - 1)])).TrimEnd();

		string before = tokens.Length >= 2 ? tokens[tokens.Length - 2] : string.Empty;
		if (before.EndsWith(')'))
			return (prefix + string.Join(' ', tokens[..(tokens.Length - 1)])).TrimEnd();

		return value;
	}

	private static bool IsNumericToken(string token)
	{
		if (token.Length == 0)
			return false;

		if (token == "-")
			return true;

		foreach (char c in token)
		{
			if (!char.IsAsciiDigit(c))
				return false;
		}

		return true;
	}

	private static bool IsValueToken(string token)
	{
		if (token.Length == 0)
			return false;

		if (token == "-")
			return true;

		// Paren groups ("(CH A/B)", "(us)", "(4 P-Core Active)") are name parts,
		// never values.
		if (token.Contains('(') || token.Contains(')'))
			return false;

		bool sawDigit = false;
		bool sawDot = false;
		for (int i = 0; i < token.Length; i++)
		{
			char c = token[i];

			if (char.IsAsciiDigit(c))
			{
				sawDigit = true;
				continue;
			}

			if (c == '.' && !sawDot && sawDigit && i + 1 < token.Length && char.IsAsciiDigit(token[i + 1]))
			{
				sawDot = true;
				continue;
			}

			if ((c == '%' || char.IsAsciiLetter(c)) && sawDigit)
			{
				bool unitToEnd = true;
				for (int j = i + 1; j < token.Length; j++)
				{
					if (!char.IsAsciiLetter(token[j]) && token[j] != '%')
					{
						unitToEnd = false;
						break;
					}
				}

				if (unitToEnd)
					return true;
			}

			return false;
		}

		return sawDigit;
	}

	private static int ParseOptionsInto(string text, List<Option> options)
	{
		text = StripComment(text);

		int selected = -1;
		int pos = 0;

		while (pos < text.Length)
		{
			int lb = text.IndexOf('[', pos);
			if (lb < 0)
				break;

			bool isSelected = lb > 0 && text[lb - 1] == '*';

			int rb = text.IndexOf(']', lb);
			if (rb < 0)
				break;

			int next = text.IndexOf('[', rb + 1);
			int labelEnd = next < 0 ? text.Length : next;

			options.Add(new Option
			{
				Index = text[(lb + 1)..rb],
				Label = text[(rb + 1)..labelEnd].Trim()
			});

			if (isSelected && selected < 0)
				selected = options.Count - 1;

			pos = labelEnd;
		}

		return selected;
	}

	private static string ParseDefault(string raw)
	{
		raw = StripComment(raw).Trim();

		if (raw.Length >= 2 && raw[0] == '[')
		{
			int close = raw.IndexOf(']');
			if (close >= 2)
			{
				string rest = raw[(close + 1)..].Trim();
				if (rest.Length > 0)
					return rest;
			}
		}

		return ExtractValue(raw);
	}

	private static string ExtractValue(string raw)
	{
		raw = StripComment(raw).Trim();

		if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
			return raw[1..^1];

		if (raw.Length >= 2 && raw[0] == '<' && raw[^1] == '>')
			return raw[1..^1];

		if (raw.Length >= 2 && raw[0] == '{' && raw[^1] == '}')
			return raw[1..^1].Trim();

		return raw;
	}

	private static string StripComment(string text)
	{
		int index = text.IndexOf("//", StringComparison.Ordinal);
		return index < 0 ? text : text[..index];
	}
}
