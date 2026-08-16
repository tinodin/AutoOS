namespace AutoOS.Core.Data.Enums.Bios;

public enum StringBlockType : byte
{
	End = 0x00,
	StringScsu = 0x10,
	StringScsuFont = 0x11,
	StringsScsu = 0x12,
	StringsScsuFont = 0x13,
	StringUcs2 = 0x14,
	StringUcs2Font = 0x15,
	StringsUcs2 = 0x16,
	StringsUcs2Font = 0x17,
	Duplicate = 0x20,
	Skip2 = 0x21,
	Skip1 = 0x22,
	StringExt1 = 0x30,
	StringExt2 = 0x31,
	StringExt4 = 0x32
}