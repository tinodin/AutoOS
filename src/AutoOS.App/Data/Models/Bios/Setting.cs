namespace AutoOS.App.Data.Models.Bios;

public sealed class Setting
{
	public int Line { get; set; }

	public int BlockStart { get; set; }

	public int BlockEnd { get; set; }

	public int ValueLineIndex { get; set; } = -1;

	public List<int> OptionLineIndexes { get; set; } = [];

	public IReadOnlyList<string>? OriginalLines { get; set; }

	public string? SetupQuestion { get; set; }

	public string? HelpString { get; set; }

	public string? Token { get; set; }

	public string? Offset { get; set; }

	public string? Width { get; set; }

	public string? BiosDefault { get; set; }

	public string? Value { get; set; }

	public List<Option> Options { get; set; } = [];

	public Option? SelectedOption { get; set; }

	public string? OriginalValue { get; set; }

	public Option? OriginalSelectedOption { get; set; }

	public bool IsRecommended { get; set; }

	public string? RecommendedValue { get; set; }

	public Option? RecommendedOption { get; set; }

	public bool HasOptions => Options.Count > 0;

	public bool HasValueField => Value != null && !HasOptions;
}
