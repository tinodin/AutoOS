using Nucs.JsonSettings;
using Nucs.JsonSettings.Autosave;
using Nucs.JsonSettings.Modulation;

namespace AutoOS.App.Common;

[Autosave]
public partial class AppConfig : NotifiyingJsonSettings, IVersionable
{
	[EnforcedVersion("1.0.0.0")]
	public Version Version { get; set; } = new Version(1, 0, 0, 0);

	public override string FileName { get; set; } = Constants.App.AppConfigPath;
}
