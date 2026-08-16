using System.Text.Json.Serialization;

namespace AutoOS.Core.Data.Models.Bios;

public sealed class BackupFile
{
	[JsonConverter(typeof(LocalDateTimeOffsetConverter))]
	public DateTimeOffset CreatedAt { get; set; }

	public string BoardManufacturer { get; set; } = string.Empty;

	public string BoardProduct { get; set; } = string.Empty;

	public string BiosVersion { get; set; } = string.Empty;

	public string BiosVersionDate { get; set; } = string.Empty;

	public List<BackupSetting> Settings { get; set; } = [];
}

public sealed class BackupSetting
{
	public string Path { get; set; } = string.Empty;

	public string Setting { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public ulong? Minimum { get; set; }

	public ulong? Maximum { get; set; }

	public uint? Increment { get; set; }

	public string Value { get; set; } = string.Empty;

	public List<string> Options { get; set; } = [];

	public string Default { get; set; } = string.Empty;

	public string VariableName { get; set; } = string.Empty;

	public string VariableGuid { get; set; } = string.Empty;

	public uint Offset { get; set; }

	public uint Width { get; set; }

	public string Token { get; set; } = string.Empty;
}