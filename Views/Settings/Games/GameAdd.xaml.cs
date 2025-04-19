using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace AutoOS.Views.Settings.Games;

public sealed partial class GameAdd : Page
{
    public string Launcher => (LauncherValue.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty;
    public string LauncherLocation => LauncherLocationValue.Text;
    public string DataLocation => DataLocationValue.Text;
    public string GameLocation => GameLocationValue.Text;
    public string GameName => GameNameValue.Text;
    public string GameCoverUrl { get; private set; } = "";
    public string GameDeveloper { get; private set; } = "Unknown";

    public GameAdd()
    {
        InitializeComponent();

        // set default ryujinx data location
        DataLocationValue.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ryujinx");

        try
        {
            // get latest file
            var latestFile = Directory.GetFiles(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games"), "*.json")
                                      .OrderByDescending(f => File.GetLastWriteTime(f))
                                      .FirstOrDefault();

            if (latestFile != null)
            {
                string json = File.ReadAllText(latestFile);
                var game = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                LauncherLocationValue.Text = game["LauncherLocation"];
                DataLocationValue.Text = game["DataLocation"];
            }
        }
        catch
        {

        }
    }

    private async void LauncherLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FilePicker(App.MainWindow);
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("Ryujinx executable", new List<string> { "*.exe" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            LauncherLocationValue.Text = file.Path;
            LauncherLocationValue.Width = double.NaN;
        }
    }

    private async void DataLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker(App.MainWindow);

        var folder = await picker.PickSingleFolderAsync();

        if (folder != null)
        {
            DataLocationValue.Text = folder.Path;
        }
    }

    private async void GameName_Changed(object sender, RoutedEventArgs e)
    {
        string name = GameNameValue.Text;

        if (string.IsNullOrWhiteSpace(name))
        {
            GameCover.Source = null;
            GameCoverUrl = "";
            GameDeveloper = "Unknown";
            return;
        }

        var result = await SearchCovers(name);

        if (result != null && result.ContainsKey("url") && Uri.IsWellFormedUriString(result["url"], UriKind.Absolute))
        {
            GameCover.Source = new BitmapImage(new Uri(result["url"]));
            GameCoverUrl = result["url"];
            GameDeveloper = result.ContainsKey("developers") ? result["developers"] : "Unknown";
        }
        else
        {
            GameCover.Source = null;
            GameCoverUrl = "";
            GameDeveloper = "Unknown";
        }
    }

    private async Task<Dictionary<string, string>> SearchCovers(string name)
    {
        string Clean(string input) => Regex.Replace(input.ToLower(), @"\s+", ".");
        string GetSearchBucket(string input)
        {
            string cleaned = Regex.Replace(input.ToLower().Substring(0, Math.Min(2, input.Length)), @"[^a-z\d]", "");
            return string.IsNullOrEmpty(cleaned) ? "@" : cleaned;
        }

        try
        {
            string searchName = Clean(name);
            string dbUrl = "https://raw.githubusercontent.com/LizardByte/GameDB/gh-pages";
            string bucket = GetSearchBucket(name);
            string bucketUrl = $"{dbUrl}/buckets/{bucket}.json";

            using var http = new HttpClient();
            var bucketJson = await http.GetStringAsync(bucketUrl);
            var maps = JsonNode.Parse(bucketJson)?.AsObject();

            var matchingIds = new List<string>();

            foreach (var kvp in maps)
            {
                var item = kvp.Value?.AsObject();
                if (item == null) continue;

                string itemName = item["name"]?.ToString() ?? "";
                if (Clean(itemName) == searchName)
                    matchingIds.Add(kvp.Key);
            }

            JsonObject maxGame = null;
            int maxFields = 0;

            foreach (var id in matchingIds)
            {
                try
                {
                    var json = await http.GetStringAsync($"{dbUrl}/games/{id}.json");
                    Debug.WriteLine($"{dbUrl}/games/{id}.json");
                    var game = JsonNode.Parse(json)?.AsObject();
                    if (game == null) continue;

                    int count = game.Where(kv => kv.Value != null && kv.Value is not JsonObject && !string.IsNullOrWhiteSpace(kv.Value.ToString())).Count();

                    if (count > maxFields)
                    {
                        maxGame = game;
                        maxFields = count;
                    }
                }
                catch { }
            }

            if (maxGame != null && maxGame["cover"] is JsonObject cover && cover["url"] != null)
            {
                string thumb = cover["url"]?.ToString() ?? "";
                int dot = thumb.LastIndexOf('.');
                int slash = thumb.LastIndexOf('/');

                if (dot >= 0 && slash >= 0)
                {
                    string slug = thumb.Substring(slash + 1, dot - slash - 1);

                    var developers = maxGame["involved_companies"]
                        ?.AsArray()
                        .Where(company => company["developer"]?.GetValue<bool>() == true)
                        .Select(company => company["company"]?["name"]?.ToString())
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToList();

                    string developerNames = developers != null && developers.Any() ? string.Join(", ", developers) : "Unknown";

                    return new Dictionary<string, string>
                        {
                            { "name", maxGame["name"]?.ToString() ?? "" },
                            { "key", $"igdb_{maxGame["id"]}" },
                            { "url", $"https://images.igdb.com/igdb/image/upload/t_cover_big_2x/{slug}.jpg" },
                            { "game_url", $"{dbUrl}/games/{maxGame["id"]}.json" },
                            { "developers", developerNames }
                        };
                }
            }
        }
        catch { }

        return null;
    }


    private async void GameLocation_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FilePicker(App.MainWindow);
        picker.ShowAllFilesOption = false;
        picker.FileTypeChoices.Add("Switch Rom", new List<string> { "*.nsp", "*.xci" });
        var file = await picker.PickSingleFileAsync();

        if (file != null)
        {
            GameLocationValue.Text = file.Path;

            string fileName = Path.GetFileNameWithoutExtension(file.Name);

            int index = fileName.IndexOfAny(new char[] { '(', '[', 'v' });
            if (index >= 0)
            {
                fileName = fileName.Substring(0, index).Trim();
            }

            GameNameValue.Text = fileName;
        }
    }
}