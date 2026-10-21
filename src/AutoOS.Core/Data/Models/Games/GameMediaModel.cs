namespace AutoOS.Core.Data.Models.Games;

public enum GameMediaType
{
	Screenshot,
	Video
}

public class GameMediaModel
{
	public GameMediaType MediaType { get; set; }
	public string? Url { get; set; }
	public string? ThumbnailUrl { get; set; }

	public static List<GameMediaModel> CreateScreenshots(IEnumerable<string?> urls)
	{
		return [.. urls.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => new GameMediaModel { Url = url, MediaType = GameMediaType.Screenshot })];
	}
}
