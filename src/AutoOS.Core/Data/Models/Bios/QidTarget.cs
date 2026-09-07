namespace AutoOS.Core.Data.Models.Bios;

public readonly record struct QidTarget(string Variable, Guid VariableGuid, ushort Offset);