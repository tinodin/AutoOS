using DevWinUI;

namespace AutoOS.Core;

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

		public static readonly Guid GlobalVariableGuid = new("8BE4DF61-93CA-11D2-AA0D-00E098032B8C");
	}
}
