namespace AutoOS.App;

public static class Constants
{
	public static class App
	{
		public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetAppDataFolderPath(), ProcessInfoHelper.ProductNameAndVersion);

		public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
	}
}