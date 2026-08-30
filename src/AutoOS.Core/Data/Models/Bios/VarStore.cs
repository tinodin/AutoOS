namespace AutoOS.Core.Data.Models.Bios;

public sealed class VarStore
{
	public ushort Id { get; set; }

	public Guid Guid { get; set; }

	public ushort Size { get; set; }

	public string Name { get; set; } = string.Empty;

	public uint HiiAttributes { get; set; } = 0xFFFFFFFF;

	public string StoreType { get; set; } = string.Empty;
}
