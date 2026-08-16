namespace AutoOS.Core.Data.Models.Bios;

public readonly record struct QidTarget(string VariableName, Guid VariableGuid, ushort Offset);