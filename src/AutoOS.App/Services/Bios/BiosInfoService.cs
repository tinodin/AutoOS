using AutoOS.App.Data.Contracts;
using AutoOS.App.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;
using AutoOS.Core.Helpers.Bios;

namespace AutoOS.App.Services.Bios;

public sealed class BiosInfoService : IBiosInfoService
{
	private static readonly string[] ProtectedChipsets = ["Z790", "B760", "H770", "X870", "X670", "B650", "A620"];
	private readonly Lazy<SmbiosInfo> _info = new(SmbiosHelper.GetInfo);

	public SmbiosInfo Info => _info.Value;

	public PageMode GetHiiState()
	{
		if (Info.BaseboardManufacturer.Contains("asus", StringComparison.OrdinalIgnoreCase))
		{
			return ProtectedChipsets.Any(chipset => Info.BaseboardProduct.Contains(chipset, StringComparison.OrdinalIgnoreCase))
				? PageMode.HiiResourcesProtected
				: PageMode.HiiResourcesRegular;
		}

		return PageMode.HiiResourcesOther;
	}

	public PageMode GetWriteProtectedState()
	{
		if (Info.BaseboardManufacturer.Contains("asus", StringComparison.OrdinalIgnoreCase))
			return PageMode.WriteProtectedAsus;
		if (Info.BaseboardManufacturer.Contains("asrock", StringComparison.OrdinalIgnoreCase))
			return PageMode.WriteProtectedAsRock;
		return PageMode.WriteProtectedOther;
	}
}
