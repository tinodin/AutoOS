using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AutoOS.Core.Data.Enums.Bios;

namespace AutoOS.Core.Helpers.Bios;

internal sealed class HiiStringTable
{
	public Dictionary<ushort, string> StringsById { get; } = [];

	public HiiStringTable(ReadOnlySpan<byte> data)
	{
		Parse(data);
	}

	private void Parse(ReadOnlySpan<byte> data)
	{
		if (data.Length < 6)
			return;
		uint stringInfoOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
		if (stringInfoOffset < 4)
			return;
		int position = (int)stringInfoOffset - 4;
		if (position < 0 || position >= data.Length)
			return;

		ushort stringId = 1;
		int guard = 0;

		while (position < data.Length && guard < 2_000_000)
		{
			guard++;
			var blockType = (StringBlockType)data[position];
			if (blockType == StringBlockType.End)
				break;

			int nextPosition;

			switch (blockType)
			{
				case StringBlockType.StringScsu:
				case StringBlockType.StringScsuFont:
					{
						int textPosition = position + 1 + (blockType == StringBlockType.StringScsuFont ? 1 : 0);
						if (textPosition >= data.Length)
							return;
						int terminator = data[textPosition..].IndexOf((byte)0);
						if (terminator < 0)
							return;
						StringsById[stringId] = Encoding.Latin1.GetString(data.Slice(textPosition, terminator));
						nextPosition = textPosition + terminator + 1;
						stringId++;
						break;
					}
				case StringBlockType.StringUcs2:
				case StringBlockType.StringUcs2Font:
					{
						int textPosition = position + 1 + (blockType == StringBlockType.StringUcs2Font ? 1 : 0);
						if (!TryReadUcs2(data, textPosition, out string? text, out int nextTextPosition))
							return;
						StringsById[stringId] = text;
						nextPosition = nextTextPosition;
						stringId++;
						break;
					}
				case StringBlockType.StringsScsu:
				case StringBlockType.StringsScsuFont:
					{
						if (position + 3 > data.Length)
							return;
						ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 1)..]);
						int textPosition = position + 3 + (blockType == StringBlockType.StringsScsuFont ? 1 : 0);
						for (int i = 0; i < count; i++)
						{
							int terminator = data[textPosition..].IndexOf((byte)0);
							if (terminator < 0)
								break;
							StringsById[stringId] = Encoding.Latin1.GetString(data.Slice(textPosition, terminator));
							textPosition = textPosition + terminator + 1;
							stringId++;
						}
						nextPosition = textPosition;
						break;
					}
				case StringBlockType.StringsUcs2:
				case StringBlockType.StringsUcs2Font:
					{
						if (position + 3 > data.Length)
							return;
						ushort count = BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 1)..]);
						int textPosition = position + 3 + (blockType == StringBlockType.StringsUcs2Font ? 1 : 0);
						for (int i = 0; i < count; i++)
						{
							if (!TryReadUcs2(data, textPosition, out string? text, out int nextTextPosition))
								break;
							StringsById[stringId] = text;
							textPosition = nextTextPosition;
							stringId++;
						}
						nextPosition = textPosition;
						break;
					}
				case StringBlockType.Duplicate:
					{
						if (position + 3 > data.Length)
							return;
						ushort referenceStringId = BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 1)..]);
						StringsById[stringId] = StringsById.TryGetValue(referenceStringId, out string? value) ? value : string.Empty;
						stringId++;
						nextPosition = position + 3;
						break;
					}
				case StringBlockType.Skip2:
					{
						if (position + 3 > data.Length)
							return;
						ushort skip = BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 1)..]);
						stringId += skip;
						nextPosition = position + 3;
						break;
					}
				case StringBlockType.Skip1:
					{
						if (position + 2 > data.Length)
							return;
						stringId += data[position + 1];
						nextPosition = position + 2;
						break;
					}
				case StringBlockType.StringExt2:
					{
						if (position + 4 > data.Length)
							return;
						byte fontStyle = data[position + 1];
						ushort length = BinaryPrimitives.ReadUInt16LittleEndian(data[(position + 2)..]);
						nextPosition = position + 4 + (fontStyle == 0x40 ? length : 0);
						break;
					}
				case StringBlockType.StringExt4:
					nextPosition = position + 6;
					break;
				case StringBlockType.StringExt1:
					nextPosition = position + 3;
					break;
				default:
					nextPosition = position + 1;
					break;
			}

			if (nextPosition <= position || nextPosition > data.Length)
				break;
			position = nextPosition;
		}
	}

	private static bool TryReadUcs2(ReadOnlySpan<byte> source, int position, [NotNullWhen(true)] out string? text, out int nextPosition)
	{
		int textStart = position;
		while (position + 1 < source.Length)
		{
			ushort codeUnit = BinaryPrimitives.ReadUInt16LittleEndian(source[position..]);
			if (codeUnit == 0)
			{
				text = Encoding.Unicode.GetString(source[textStart..position]);
				nextPosition = position + 2;
				return true;
			}
			position += 2;
		}
		text = null;
		nextPosition = position;
		return false;
	}
}
