namespace AutoOS.Core.Data.Enums.Bios;

public enum AmiSmmStatus : uint
{
	BufferTooSmallWire = 0x8500,
	NotFound = 0x8F00,
	PasswordMismatch = 0x8200,
	PasswordRetryExceeded = 0x8600,
	InvalidPassword = 0x9A00
}
