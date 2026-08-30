using AutoOS.Core.Data.Enums.Bios;

namespace AutoOS.Core.Helpers.Bios;

public static class SmmStatusHelper
{
	public static string Format(EfiStatus status) => status switch
	{
		EfiStatus.EFI_SUCCESS => "EFI_SUCCESS (0x0)",
		EfiStatus.EFI_INVALID_PARAMETER => "EFI_INVALID_PARAMETER (0x02)",
		EfiStatus.EFI_BAD_BUFFER_SIZE => "EFI_BAD_BUFFER_SIZE (0x04)",
		EfiStatus.EFI_BUFFER_TOO_SMALL => "EFI_BUFFER_TOO_SMALL (0x05)",
		EfiStatus.EFI_WRITE_PROTECTED => "EFI_WRITE_PROTECTED (0x08)",
		EfiStatus.EFI_SECURITY_VIOLATION => "EFI_SECURITY_VIOLATION (0x1A)",
		EfiStatus.EFI_NOT_FOUND => "EFI_NOT_FOUND (0x0E)",
		_ => $"EFI status 0x{(ulong)status:X}"
	};

	public static string Format(AmiSmmStatus status) => status switch
	{
		AmiSmmStatus.BufferTooSmallWire => "AMI_BUFFER_TOO_SMALL (0x8500)",
		AmiSmmStatus.NotFound => "AMI_NOT_FOUND (0x8F00)",
		AmiSmmStatus.PasswordMismatch => "AMI_PASSWORD_MISMATCH (0x8200)",
		AmiSmmStatus.PasswordRetryExceeded => "AMI_PASSWORD_RETRY_EXCEEDED (0x8600)",
		AmiSmmStatus.InvalidPassword => "AMI_INVALID_PASSWORD (0x9A00)",
		AmiSmmStatus.PasswordUnlockSuccess => "AMI_PASSWORD_UNLOCK_SUCCESS (0xDF00)",
		_ => $"AMI status 0x{(uint)status:X}"
	};

	public static string Format(uint raw)
	{
		if (Enum.IsDefined(typeof(AmiSmmStatus), raw))
			return Format((AmiSmmStatus)raw);

		if (Enum.IsDefined(typeof(EfiStatus), (ulong)raw))
			return Format((EfiStatus)(ulong)raw);

		return $"SMM status 0x{raw:X}";
	}
}
