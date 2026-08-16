using AutoOS.App.Data.Enums.Bios;
using AutoOS.Core.Data.Models.Bios;

namespace AutoOS.App.Data.Contracts;

public interface IBiosInfoService
{
	SmbiosInfo Info { get; }

	PageMode GetHiiState();

	PageMode GetWriteProtectedState();
}