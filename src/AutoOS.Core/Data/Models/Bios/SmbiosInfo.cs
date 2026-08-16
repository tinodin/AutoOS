namespace AutoOS.Core.Data.Models.Bios;

public sealed class SmbiosInfo
{
	public string BiosVendor { get; set; } = string.Empty;

	public string BiosVersion { get; set; } = string.Empty;

	public string BiosReleaseDate { get; set; } = string.Empty;

	public string SystemManufacturer { get; set; } = string.Empty;

	public string SystemProduct { get; set; } = string.Empty;

	public string BaseboardManufacturer { get; set; } = string.Empty;

	public string BaseboardProduct { get; set; } = string.Empty;
}