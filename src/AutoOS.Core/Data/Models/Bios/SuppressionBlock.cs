namespace AutoOS.Core.Data.Models.Bios;

internal sealed class SuppressionBlock
{
	public List<SuppressionToken> Tokens { get; } = [];
}

internal readonly record struct SuppressionToken(byte Opcode, ushort Qid, ulong Value, IReadOnlyList<ushort>? Values = null);
