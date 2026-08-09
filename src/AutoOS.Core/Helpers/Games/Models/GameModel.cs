namespace AutoOS.Core.Helpers.Games.Models;

public class GameModel
{
	public string Id { get; set; } = string.Empty;
	public string? ImageUrl { get; set; }
	public string? BackgroundImageUrl { get; set; }
	public string? Title { get; set; }
	public string? Developers { get; set; }
	public bool UpdateIsAvailable { get; set; }
	public double Rating { get; set; }
	public string PlayTime { get; set; } = "0m";
	public string? AgeRatingUrl { get; set; }
	public string? AgeRatingTitle { get; set; }
	public string? AgeRatingDescription { get; set; }
	public string? Elements { get; set; }
	public List<string> Genres { get; set; } = [];
	public List<string> Features { get; set; } = [];
	public string? Description { get; set; }
	public List<string> Screenshots { get; set; } = [];
	public List<string> VideoUrls { get; set; } = [];
	public string? InstallLocation { get; set; }
	public string? Launcher { get; set; }
	public string? CatalogItemId { get; set; }
	public string? CatalogNamespace { get; set; }
	public string? AppName { get; set; }
	public string? LaunchCommand { get; set; }
	public string? LaunchExecutable { get; set; }
	public List<string> ProcessNames { get; set; } = [];
	public List<string> BackgroundProcessNames { get; set; } = [];
	public string? ArtifactId { get; set; }
	public string? GameID { get; set; }
	public string? LauncherLocation { get; set; }
	public string? DataLocation { get; set; }
	public string? GameLocation { get; set; }
	public string? ReleaseDate { get; set; }
	public string? Size { get; set; }
	public string? Version { get; set; }
}
