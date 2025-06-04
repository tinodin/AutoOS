using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;

namespace AutoOS.Helpers
{
    public static class CustomGameHelper
    {
        public static async Task LoadGames()
        {
            var gamesDir = Path.Combine(PathHelper.GetAppDataFolderPath(), "Games");
            if (Directory.Exists(gamesDir))
            {
                var files = Directory.GetFiles(gamesDir, "*.json");

                foreach (var file in files)
                {
                    // read game json
                    var json = await Task.Run(() => File.ReadAllText(file));
                    var game = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    // add game panel
                    var gamePanel = new GamePanel
                    {
                        Launcher = game["Launcher"],
                        LauncherLocation = game["LauncherLocation"],
                        DataLocation = game["DataLocation"],
                        InstallLocation = game["GameLocation"],
                        Title = game["GameName"],
                        Description = game["GameDeveloper"],
                        ImageSource = new BitmapImage(new Uri(game["GameCoverUrl"]))
                    };

                    ((StackPanel)GamesPage.Instance.Games.HeaderContent).Children.Add(gamePanel);

                    // start watcher
                    gamePanel.CheckGameRunning();
                }
            }
        }
    }
}