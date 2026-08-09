namespace AutoOS.App.Data.Models.Bios;

public sealed class BiosFile
{
	public required IReadOnlyList<string> Lines { get; init; }

	public required IReadOnlyList<Setting> Settings { get; init; }

	public required int HeaderEnd { get; init; }
}
