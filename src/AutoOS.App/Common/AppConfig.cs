using Nucs.JsonSettings;
using Nucs.JsonSettings.Modulation;

namespace AutoOS.App.Common;

[GenerateAutoSaveOnChange]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
	[EnforcedVersion("1.0.0.0")]
	public Version Version { get; set; } = new Version(1, 0, 0, 0);

	private string fileName { get; set; } = Constants.App.AppConfigPath;


	// Docs: https://github.com/Nucs/JsonSettings
}
