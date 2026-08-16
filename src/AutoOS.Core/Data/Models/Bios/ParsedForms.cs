namespace AutoOS.Core.Data.Models.Bios;

public sealed class ParsedForms
{
	public Dictionary<ushort, VarStore> VarStores { get; set; } = [];

	public List<Question> Questions { get; set; } = [];

	public Dictionary<ushort, string> FormTitles { get; set; } = [];

	public List<(ushort Parent, ushort Target, string Label)> FormReferences { get; set; } = [];

	public List<ushort> FormOrder { get; set; } = [];

	public Dictionary<ushort, List<FormItem>> FormItems { get; set; } = [];
}