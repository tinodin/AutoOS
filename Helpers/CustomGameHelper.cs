using AutoOS.Views.Settings.Games;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;

namespace AutoOS.Helpers
{
    public static class CustomGameHelper
    {

        public static void LoadGames()
        {
            if (Directory.Exists(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games")))
            {
                foreach (var file in Directory.GetFiles(Path.Combine(PathHelper.GetAppDataFolderPath(), "Games"), "*.json"))
                {
                    // read game json
                    var game = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file));

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