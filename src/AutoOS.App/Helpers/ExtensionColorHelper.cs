using Windows.UI;

namespace AutoOS.App.Helpers;

public static class ExtensionColorHelper
{
	private static readonly Color NO_EXTENSION_COLOR = Color.FromArgb(255, 160, 160, 160);

	// Per-extension palette for the extension breakdown legend. Extensions hash
	// into this palette, so each extension always maps to the same colour.
	private static readonly Color[] EXTENSION_PALETTE =
	[
		Color.FromArgb(255, 232, 119, 12),
		Color.FromArgb(255, 139, 195, 74),
		Color.FromArgb(255, 156, 39, 176),
		Color.FromArgb(255, 0, 172, 193),
		Color.FromArgb(255, 236, 64, 122),
		Color.FromArgb(255, 255, 214, 0),
		Color.FromArgb(255, 229, 57, 53),
		Color.FromArgb(255, 67, 160, 71),
		Color.FromArgb(255, 66, 133, 244),
		Color.FromArgb(255, 245, 124, 0),
		Color.FromArgb(255, 0, 137, 123),
		Color.FromArgb(255, 171, 71, 188),
		Color.FromArgb(255, 216, 27, 96),
		Color.FromArgb(255, 124, 179, 66),
		Color.FromArgb(255, 255, 143, 0),
		Color.FromArgb(255, 3, 155, 229),
		Color.FromArgb(255, 141, 110, 99),
		Color.FromArgb(255, 120, 144, 156),
		Color.FromArgb(255, 174, 213, 0),
		Color.FromArgb(255, 255, 112, 67),
		Color.FromArgb(255, 77, 182, 172),
		Color.FromArgb(255, 186, 104, 200),
		Color.FromArgb(255, 251, 192, 45),
		Color.FromArgb(255, 142, 36, 170),
		Color.FromArgb(255, 0, 200, 83),
		Color.FromArgb(255, 255, 61, 0),
		Color.FromArgb(255, 92, 107, 192),
		Color.FromArgb(255, 0, 121, 107)
	];

	// The extensions WizTree surfaces most often get pinned palette slots, so the top
	// legend rows (and their treemap tiles) are always mutually distinct. Anything
	// else hashes into the palette.
	private static readonly Dictionary<string, int> FIXED_EXTENSION_PALETTE_INDEX = new(StringComparer.OrdinalIgnoreCase)
	{
		[".dll"] = 0,
		[".sys"] = 1,
		[".exe"] = 2,
		[".msi"] = 3,
		[".cab"] = 4,
		[".nupkg"] = 5,
		[".7z"] = 6,
		[".pdf"] = 7,
		[".jar"] = 8,
		[".zip"] = 9,
		[".pdb"] = 10,
		[".vsix"] = 11,
		[".vhdx"] = 12,
		[".vhd"] = 13,
		[".dat"] = 14,
		[".xml"] = 15,
		[".dmp"] = 16,
		[".bin"] = 17,
		[".pak"] = 18,
		[".asar"] = 19,
		[".js"] = 20,
		[".wim"] = 21,
		[".esd"] = 22,
		[".sqlite"] = 23,
		[".vmdk"] = 24,
		[".iso"] = 25,
		[".mp4"] = 26,
		[".lib"] = 27
	};

	public static Color GetColor(string extension)
	{
		if (IsNoExtension(extension))
			return NO_EXTENSION_COLOR;

		string normalized = NormalizeExtension(extension);
		if (normalized.Length == 0)
			return NO_EXTENSION_COLOR;

		if (FIXED_EXTENSION_PALETTE_INDEX.TryGetValue(normalized, out int fixedIndex))
			return EXTENSION_PALETTE[fixedIndex];

		uint hash = Fnv1a32(normalized);
		return EXTENSION_PALETTE[hash % (uint)EXTENSION_PALETTE.Length];
	}

	public static bool IsNoExtension(string extension)
	{
		if (string.IsNullOrWhiteSpace(extension))
			return true;

		return extension.Trim().Equals("(No Extension)", StringComparison.OrdinalIgnoreCase);
	}

	public static string NormalizeExtension(string extension)
	{
		if (IsNoExtension(extension))
			return string.Empty;

		string normalized = extension.Trim().ToLowerInvariant();
		if (normalized.Length == 0 || normalized.StartsWith('.'))
			return normalized;

		return string.Concat(".", normalized);
	}

	private static uint Fnv1a32(string value)
	{
		uint hash = 2166136261u;
		foreach (char c in value)
		{
			hash ^= c;
			hash *= 16777619u;
		}

		return hash;
	}
}
