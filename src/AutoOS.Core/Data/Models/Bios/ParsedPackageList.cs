namespace AutoOS.Core.Data.Models.Bios;

public sealed class ParsedPackageList
{
	public Guid Guid { get; set; }

	public Dictionary<ushort, VarStore> VarStores { get; set; } = [];

	public List<Question> Questions { get; set; } = [];

	public List<HiiLanguage> Languages { get; set; } = [];
}