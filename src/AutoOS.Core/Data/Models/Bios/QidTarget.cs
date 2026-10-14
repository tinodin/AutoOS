namespace AutoOS.Core.Data.Models.Bios;

public readonly record struct QidTarget(string VarStoreName, Guid VarStoreGuid, ushort Offset);