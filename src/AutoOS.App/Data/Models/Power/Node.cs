using System.Diagnostics.CodeAnalysis;
using AutoOS.App.Data.Enums.Power;
using AutoOS.Core.Data.Models.Power;

namespace AutoOS.App.Data.Models.Power;

public sealed partial class Node : ObservableObject, IOrderedNode
{
	[DynamicDependency(nameof(IsExpanded), typeof(Node))]
	[DynamicDependency(nameof(Children), typeof(Node))]
	[DynamicDependency(nameof(DisplayName), typeof(Node))]
	[DynamicDependency(nameof(DisplayAc), typeof(Node))]
	[DynamicDependency(nameof(DisplayDc), typeof(Node))]
	[DynamicDependency(nameof(DisplayCompare1Ac), typeof(Node))]
	[DynamicDependency(nameof(DisplayCompare1Dc), typeof(Node))]
	[DynamicDependency(nameof(DisplayCompare2Ac), typeof(Node))]
	[DynamicDependency(nameof(DisplayCompare2Dc), typeof(Node))]
	[DynamicDependency(nameof(DisplayOriginalAc), typeof(Node))]
	[DynamicDependency(nameof(DisplayOriginalDc), typeof(Node))]
	internal Node(
		NodeKind nodeKind,
		string displayName,
		string description,
		Guid guid,
		Setting? setting = null,
		SettingState? state = null,
		string? baseDisplayName = null)
	{
		NodeKind = nodeKind;
		DisplayName = displayName;
		BaseDisplayName = baseDisplayName ?? displayName;
		Description = description;
		Guid = guid;
		Setting = setting;
		State = state;
	}

	public NodeKind NodeKind { get; }

	public Setting? Setting { get; }

	public Guid Guid { get; }

	public string Description { get; }

	public bool IsExpanded { get; set; } = true;

	public ObservableCollection<Node> Children { get; } = [];

	internal int Order { get; set; }

	int IOrderedNode.Order => Order;

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	public string BaseDisplayName { get; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayAc))]
	[NotifyPropertyChangedFor(nameof(DisplayDc))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginalAc))]
	[NotifyPropertyChangedFor(nameof(DisplayOriginalDc))]
	[NotifyPropertyChangedFor(nameof(DisplayCompare1Ac))]
	[NotifyPropertyChangedFor(nameof(DisplayCompare1Dc))]
	[NotifyPropertyChangedFor(nameof(DisplayCompare2Ac))]
	[NotifyPropertyChangedFor(nameof(DisplayCompare2Dc))]
	[NotifyPropertyChangedFor(nameof(IsAcModified))]
	[NotifyPropertyChangedFor(nameof(IsDcModified))]
	[NotifyPropertyChangedFor(nameof(IsAcDifferent))]
	[NotifyPropertyChangedFor(nameof(IsDcDifferent))]
	[NotifyPropertyChangedFor(nameof(AcToolTip))]
	[NotifyPropertyChangedFor(nameof(DcToolTip))]
	[NotifyPropertyChangedFor(nameof(OriginalAcToolTip))]
	[NotifyPropertyChangedFor(nameof(OriginalDcToolTip))]
	[NotifyPropertyChangedFor(nameof(Compare1AcToolTip))]
	[NotifyPropertyChangedFor(nameof(Compare1DcToolTip))]
	[NotifyPropertyChangedFor(nameof(Compare2AcToolTip))]
	[NotifyPropertyChangedFor(nameof(Compare2DcToolTip))]
	[NotifyPropertyChangedFor(nameof(EditAcToolTip))]
	[NotifyPropertyChangedFor(nameof(EditDcToolTip))]
	public partial SettingState? State { get; set; }

	public bool IsAcModified => State?.IsAcModified ?? false;

	public bool IsDcModified => State?.IsDcModified ?? false;

	public bool IsAcDifferent => State?.IsAcDifferent ?? false;

	public bool IsDcDifferent => State?.IsDcDifferent ?? false;

	public string DisplayAc => State?.DisplayAc ?? string.Empty;

	public string DisplayDc => State?.DisplayDc ?? string.Empty;

	public string AcToolTip => State?.AcToolTip ?? string.Empty;

	public string DcToolTip => State?.DcToolTip ?? string.Empty;

	public string DisplayCompare1Ac => State?.DisplayCompare1Ac ?? string.Empty;

	public string DisplayCompare1Dc => State?.DisplayCompare1Dc ?? string.Empty;

	public string DisplayCompare2Ac => State?.DisplayCompare2Ac ?? string.Empty;

	public string DisplayCompare2Dc => State?.DisplayCompare2Dc ?? string.Empty;

	public string Compare1AcToolTip => State?.Compare1AcToolTip ?? string.Empty;

	public string Compare1DcToolTip => State?.Compare1DcToolTip ?? string.Empty;

	public string Compare2AcToolTip => State?.Compare2AcToolTip ?? string.Empty;

	public string Compare2DcToolTip => State?.Compare2DcToolTip ?? string.Empty;

	public string DisplayOriginalAc => State?.DisplayOriginalAc ?? string.Empty;

	public string DisplayOriginalDc => State?.DisplayOriginalDc ?? string.Empty;

	public string OriginalAcToolTip => State?.OriginalAcToolTip ?? string.Empty;

	public string OriginalDcToolTip => State?.OriginalDcToolTip ?? string.Empty;

	public string EditAcToolTip => State?.EditAcToolTip ?? string.Empty;

	public string EditDcToolTip => State?.EditDcToolTip ?? string.Empty;

	public IReadOnlyList<Option>? Options => Setting?.Options;

	public bool IsAdjustable => Setting is { Options.Count: > 0 } || (Setting != null && Setting.Minimum.HasValue && Setting.Maximum.HasValue && Setting.Increment.HasValue);

	partial void OnStateChanged(SettingState? value)
	{
		if (value != null)
			value.PropertyChanged += OnStatePropertyChanged;
	}

	private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		OnPropertyChanged(e.PropertyName ?? string.Empty);
	}
}
