namespace AutoOS.Core.Data.Models.Sound;

public partial class AudioFormatOption
{
	public uint SampleRate { get; set; }
	public ushort Bits { get; set; }
	public ushort Channels { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public bool IsCurrent { get; set; }
	public ushort ActualBitsPerSample { get; set; }
	public Guid SubFormat { get; set; }
	public override string ToString() => DisplayName;
}

public partial class AudioDetails
{
	public float CurrentVolume { get; set; }
	public bool IsMuted { get; set; }
	public uint CurrentSampleRate { get; set; }
	public ushort CurrentBitDepth { get; set; }
	public ushort CurrentChannels { get; set; }
	public float LeftVolume { get; set; }
	public float RightVolume { get; set; }
	public bool SupportPerChannelVolume { get; set; } = true;
	public List<AudioFormatOption> Formats { get; set; } = [];
}

public partial class BufferSizeOption
{
	public uint Frames { get; set; }
	public float Ms { get; set; }
	public string DisplayName { get; set; } = string.Empty;
	public bool IsCurrent { get; set; }
	public bool IsDefault { get; set; }
	public override string ToString() => DisplayName;
}
