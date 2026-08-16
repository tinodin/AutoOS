namespace AutoOS.Core.Data.Models.Bios;

public sealed class PackageList
{
	public Guid Guid { get; set; }

	public ReadOnlyMemory<byte> Payload { get; set; }
}