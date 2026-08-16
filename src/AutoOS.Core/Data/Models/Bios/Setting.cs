using AutoOS.Core.Data.Enums.Bios;

namespace AutoOS.Core.Data.Models.Bios;

public sealed class Setting
{
	public string VariableName { get; init; } = string.Empty;

	public Guid VariableGuid { get; init; } = Guid.Empty;

	public uint Offset { get; init; }

	public uint Width { get; init; }

	public uint VarStoreSize { get; init; }

	public string Token { get; init; } = string.Empty;

	public List<string> Flags { get; init; } = [];

	public string Path { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string Description { get; init; } = string.Empty;

	public ulong Minimum { get; init; }

	public ulong Maximum { get; init; }

	public uint Increment { get; init; }

	public IfrNumericFormat NumericFormat { get; init; } = IfrNumericFormat.Dec;

	public string Value { get; set; } = string.Empty;

	public List<Option> Options { get; init; } = [];

	internal List<List<SuppressionBlock>?>? SuppressionBlocks { get; init; }

	public string Default { get; init; } = string.Empty;

	public string? RecommendedValue { get; set; }

	public Option? RecommendedOption { get; set; }
}
