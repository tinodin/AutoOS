namespace AutoOS.Core.Data.Models.Bios;

public sealed class VarStore
{
	public ushort Id { get; set; }

	public Guid Guid { get; set; }

	public ushort Size { get; set; }

	public string Name { get; set; } = string.Empty;
}