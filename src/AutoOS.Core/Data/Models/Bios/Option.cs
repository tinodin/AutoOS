namespace AutoOS.Core.Data.Models.Bios;

public sealed class Option
{
	public string Index { get; set; } = string.Empty;

	public string Label { get; set; } = string.Empty;

	public string StoredValue { get; set; } = string.Empty;

	public ulong Value { get; set; }

	public bool IsDefault { get; set; }
}