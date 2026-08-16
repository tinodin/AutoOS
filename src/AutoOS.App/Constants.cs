namespace AutoOS.App;

public static class Constants
{
	public static class App
	{
		public static readonly string RootDirectoryPath = Path.Combine(PathHelper.GetAppDataFolderPath(), ProcessInfoHelper.ProductNameAndVersion);

		public static readonly string AppConfigPath = Path.Combine(RootDirectoryPath, "AppConfig.json");
	}

	public static class Bios
	{
		public static readonly Guid SecureBootVarStoreGuid = new("7B59104A-C00D-4158-87FF-F04D6396A915");
	}
}