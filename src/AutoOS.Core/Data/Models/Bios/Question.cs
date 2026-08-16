using AutoOS.Core.Data.Enums.Bios;

namespace AutoOS.Core.Data.Models.Bios;

public sealed class Question
{
	public IfrOpcode Opcode { get; set; }

	public ushort VarStoreId { get; set; }

	public ushort Offset { get; set; }

	public uint Width { get; set; }

	public string Prompt { get; set; } = string.Empty;

	public string Help { get; set; } = string.Empty;

	public byte Flags { get; set; }

	public ushort FormId { get; set; }

	public string Path { get; set; } = string.Empty;

	public string Token { get; set; } = string.Empty;

	public ulong? Minimum { get; set; }

	public ulong? Maximum { get; set; }

	public ulong? Step { get; set; }

	public ulong? DefaultValue { get; set; }

	public IfrNumericFormat NumericFormat { get; set; } = IfrNumericFormat.Dec;

	public List<Option> Options { get; } = [];

	internal List<List<SuppressionBlock>?>? SuppressionBlocks { get; set; }
}