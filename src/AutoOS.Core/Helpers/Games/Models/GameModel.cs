using System.Globalization;

namespace AutoOS.Core.Helpers.Games;

public class GameModel
{
    public string Id { get; set; } = string.Empty;
    public string ImageUrl { get; set; }
    public string BackgroundImageUrl { get; set; }
    public string Title { get; set; }
    public string Developers { get; set; }
    public bool UpdateIsAvailable { get; set; }
    public double Rating { get; set; }
    public string PlayTime { get; set; } = "0m";
    public string AgeRatingUrl { get; set; }
    public string AgeRatingTitle { get; set; }
    public string AgeRatingDescription { get; set; }
    public string Elements { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public string Description { get; set; }
    public List<string> Screenshots { get; set; } = new();
    public List<string> VideoUrls { get; set; } = new();
    public string InstallLocation { get; set; }
    public string Launcher { get; set; }
    public string CatalogItemId { get; set; }
    public string CatalogNamespace { get; set; }
    public string AppName { get; set; }
    public string LaunchCommand { get; set; }
    public string LaunchExecutable { get; set; }
    public List<string> ProcessNames { get; set; } = new();
    public List<string> BackgroundProcessNames { get; set; } = new();
    public string ArtifactId { get; set; }
    public string GameID { get; set; }
    public string LauncherLocation { get; set; }
    public string DataLocation { get; set; }
    public string GameLocation { get; set; }
    public string ReleaseDate { get; set; }
    public string Size { get; set; }
    public string Version { get; set; }

    public static string PlaytimeFormat { get; set; } = "Compact";
    public static event Action? PlaytimeFormatChanged;

    public static void SetPlaytimeFormat(string format)
    {
        PlaytimeFormat = format;
        PlaytimeFormatChanged?.Invoke();
    }

    public static string FormatPlayTime(int totalMinutes, string format = null)
    {
        const int twoHoursInMinutes = 120;
        const int thousandHoursInMinutes = 60000;

        format ??= PlaytimeFormat;

        if (format == "Compact")
        {
            // Original format: 1h 1m
            var ts = TimeSpan.FromMinutes(totalMinutes);
            return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m";
        }
        else
        {
            // Detailed format: Steam Format
            if (totalMinutes == 0)
            {
                return "0 minutes played";
            }
            else if (totalMinutes < twoHoursInMinutes)
            {
                return totalMinutes == 1 ? "1 minute played" : $"{totalMinutes} minutes played";
            }
            else if (totalMinutes < thousandHoursInMinutes)
            {
                double hours = totalMinutes / 60.0;
                string timeStr = $"{hours.ToString("F1", CultureInfo.InvariantCulture)} hours played";
                // Remove .0 if present
                if (timeStr.EndsWith(".0 hours played"))
                {
                    timeStr = timeStr.Replace(".0 hours played", " hours played");
                }
                return timeStr;
            }
            else
            {
                int hours = totalMinutes / 60;
                return $"{hours} hours played";
            }
        }
    }
}
