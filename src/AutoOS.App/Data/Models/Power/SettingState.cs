namespace AutoOS.App.Data.Models.Power;

public sealed class SettingState
{
	public uint AcValue { get; set; }

	public uint DcValue { get; set; }

	public uint OriginalAcValue { get; set; }

	public uint OriginalDcValue { get; set; }

	public string EditAcValue { get; set; } = string.Empty;

	public string EditDcValue { get; set; } = string.Empty;

	public Option? EditAcOption { get; set; }

	public Option? EditDcOption { get; set; }

	public bool IsModified => AcValue != OriginalAcValue || DcValue != OriginalDcValue;
}