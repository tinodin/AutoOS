using System.Collections.ObjectModel;
using AutoOS.App.Data.Enums.Power;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoOS.App.Data.Models.Power;

public sealed partial class Node : ObservableObject
{
	internal Node(
		NodeKind nodeKind,
		PageMode mode,
		string displayName,
		string description,
		Guid guid,
		Setting? setting = null,
		SettingState? state = null,
		string? baseDisplayName = null)
	{
		NodeKind = nodeKind;
		Mode = mode;
		DisplayName = displayName;
		BaseDisplayName = baseDisplayName ?? displayName;
		Description = description;
		Guid = guid;
		Setting = setting;
		State = state;
	}

	public NodeKind NodeKind { get; }

	public PageMode Mode { get; }

	public Setting? Setting { get; }

	public Guid Guid { get; }

	public string Description { get; }

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	public string BaseDisplayName { get; }

	public SettingState? State { get; }

	public bool HasValues => NodeKind == NodeKind.Setting;

	public uint AcValue => State?.AcValue ?? 0;

	public uint DcValue => State?.DcValue ?? 0;

	public uint OriginalAcValue => State?.OriginalAcValue ?? 0;

	public uint OriginalDcValue => State?.OriginalDcValue ?? 0;

	public bool IsModified => State?.IsModified ?? false;

	public bool IsAcDifferent => Mode switch
	{
		PageMode.Comparison => State?.IsAcDifferent ?? false,
		PageMode.ViewChanges => AcValue != OriginalAcValue,
		_ => false
	};

	public bool IsDcDifferent => Mode switch
	{
		PageMode.Comparison => State?.IsDcDifferent ?? false,
		PageMode.ViewChanges => DcValue != OriginalDcValue,
		_ => false
	};

	public string DisplayAc => State?.DisplayAc ?? string.Empty;

	public string DisplayDc => State?.DisplayDc ?? string.Empty;

	public string AcToolTip => State?.AcToolTip ?? string.Empty;

	public string DcToolTip => State?.DcToolTip ?? string.Empty;

	public string DisplayCompareAc => State?.DisplayCompareAc ?? string.Empty;

	public string DisplayCompareDc => State?.DisplayCompareDc ?? string.Empty;

	public string CompareAcToolTip => State?.CompareAcToolTip ?? string.Empty;

	public string CompareDcToolTip => State?.CompareDcToolTip ?? string.Empty;

	public string DisplayOriginalAc => State?.DisplayOriginalAc ?? string.Empty;

	public string DisplayOriginalDc => State?.DisplayOriginalDc ?? string.Empty;

	public string OriginalAcToolTip => State?.OriginalAcToolTip ?? string.Empty;

	public string OriginalDcToolTip => State?.OriginalDcToolTip ?? string.Empty;

	public string EditAcToolTip => State?.EditAcToolTip ?? string.Empty;

	public string EditDcToolTip => State?.EditDcToolTip ?? string.Empty;

	public IReadOnlyList<Option>? Options => Setting?.Options;

	public bool HasOptions => Setting is { Options.Count: > 0 };

	public bool IsAdjustable => HasOptions || (Setting != null && Setting.Minimum.HasValue && Setting.Maximum.HasValue && Setting.Increment.HasValue);
}
