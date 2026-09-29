using AutoOS.App.Data.Commands;
using AutoOS.App.Data.Enums;
using AutoOS.App.Helpers.TreeGrid;
using AutoOS.App.Services;
using AutoOS.App.ViewModels.Dialogs.Scheduling;
using AutoOS.App.Views.Settings.Scheduling;
using AutoOS.Core.Data.Models.CPU;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Helpers.CPU;
using AutoOS.Core.Helpers.Device;
using AutoOS.Core.Helpers.Scheduling;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Syncfusion.UI.Xaml.TreeGrid;

namespace AutoOS.App.Views.Settings;

public sealed partial class SchedulingPage : Page
{
	public ObservableCollection<SchedulingGroup> Nodes { get; } = [];

	private CpuSetsInfo _cpuSetsInfo = null!;

	private readonly CopyTextCommand _copyTextCommand = new();

	private readonly Stack<Dictionary<string, DeviceState>> _undoStates = [];

	private readonly Stack<Dictionary<string, DeviceState>> _redoStates = [];

	private Dictionary<string, DeviceState> _initialState = [];

	private record struct DeviceState(uint MsiSupported, uint MsiLimit, uint MaxMsiLimit, uint DevicePolicy, uint DevicePriority, ulong AssignmentSetOverride);

	public SchedulingPage()
	{
		InitializeComponent();
		Loaded += SchedulingPage_Loaded;
	}

	private void SchedulingPage_Loaded(object sender, RoutedEventArgs e)
	{
		_cpuSetsInfo = CpuHelper.GetCpuSets();
		Nodes.Clear();

		SchedulingGroup audioGroup = new() { Name = "Audio Controllers", IsExpanded = true, DeviceType = DeviceType.AudioController };
		LoadDeviceGroup(DeviceType.AudioController, audioGroup);
		if (audioGroup.SubItems.Count > 0)
			Nodes.Add(audioGroup);

		SchedulingGroup gpuGroup = new() { Name = "Graphics Cards", IsExpanded = true, DeviceType = DeviceType.GPU };
		LoadDeviceGroup(DeviceType.GPU, gpuGroup);
		if (gpuGroup.SubItems.Count > 0)
			Nodes.Add(gpuGroup);

		SchedulingGroup xhciGroup = new() { Name = "XHCI Controllers", IsExpanded = true, DeviceType = DeviceType.XHCI };
		LoadDeviceGroup(DeviceType.XHCI, xhciGroup);
		if (xhciGroup.SubItems.Count > 0)
			Nodes.Add(xhciGroup);

		SchedulingGroup nicGroup = new() { Name = "Network Interface Controllers", IsExpanded = true, DeviceType = DeviceType.NIC };
		LoadDeviceGroup(DeviceType.NIC, nicGroup);
		if (nicGroup.SubItems.Count > 0)
			Nodes.Add(nicGroup);

		_initialState = CaptureState();
		_undoStates.Clear();
		_redoStates.Clear();
		UpdateToolbarState();
	}

	private static void LoadDeviceGroup(DeviceType type, SchedulingGroup group)
	{
		List<DeviceInfo> devices = DeviceHelper.GetDevices(type);

		List<SchedulingItem> items = devices
			.Where(device => device.SupportsIrq)
			.Select(device => new SchedulingItem
			{
				DeviceType = type,
				DeviceDescription = device.DeviceDescription,
				FriendlyName = device.FriendlyName,
				PnpDeviceId = device.PnpDeviceId,
				Location = device.Location,
				MsiSupported = device.MsiSupported,
				MsiLimit = device.MsiLimit,
				MaxMsiLimit = device.MaxMsiLimit,
				DevicePolicy = device.DevicePolicy,
				DevicePriority = device.DevicePriority,
				AssignmentSetOverride = device.AssignmentSetOverride,
			})
			.OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

		foreach (SchedulingItem? item in items)
			group.SubItems.Add(item);
	}

	public void UpdateDevice(DeviceType deviceType, string pnpDeviceId, DeviceInfo? targetDevice = null)
	{
		SchedulingItem? item = Nodes.SelectMany(g => g.SubItems).FirstOrDefault(d => string.Equals(d.PnpDeviceId, pnpDeviceId, StringComparison.OrdinalIgnoreCase));
		if (item == null)
			return;

		targetDevice ??= DeviceHelper.GetDevices(item.DeviceType).FirstOrDefault(d => string.Equals(d.PnpDeviceId, pnpDeviceId, StringComparison.OrdinalIgnoreCase));
		if (targetDevice == null)
			return;

		item.MsiSupported = targetDevice.MsiSupported;
		item.MsiLimit = targetDevice.MsiLimit;
		item.MaxMsiLimit = targetDevice.MaxMsiLimit;
		item.DevicePolicy = targetDevice.DevicePolicy;
		item.DevicePriority = targetDevice.DevicePriority;
		item.AssignmentSetOverride = targetDevice.AssignmentSetOverride;
	}

	private async void Optimize_Click(object sender, RoutedEventArgs e)
	{
		List<SchedulingItem> allDevices = Nodes.SelectMany(group => group.SubItems).ToList();
		if (allDevices.Count == 0)
			return;

		var viewModel = new OptimizeSchedulingDialogViewModel(allDevices);

		IDialogService? dialogService = Ioc.Default.GetService<IDialogService>();
		DialogResult result = dialogService != null
			? await dialogService.ShowDialogAsync(viewModel)
			: (DialogResult)await new Dialogs.Scheduling.OptimizeSchedulingDialog { ViewModel = viewModel, XamlRoot = XamlRoot }.ShowAsync();

		if (result != DialogResult.Primary || viewModel.SelectedDevices.Count == 0)
			return;

		Dictionary<string, DeviceState> previousState = CaptureState();

		List<(DeviceType DeviceType, string PnpDeviceId)> selected = viewModel.SelectedDevices
			.Select(item => (item.DeviceType, item.PnpDeviceId))
			.ToList();

		await SchedulingHelper.OptimizeAffinities(selected, onDeviceUpdated: UpdateDevice);

		Dictionary<string, DeviceState> currentState = CaptureState();

		if (!AreStatesEqual(previousState, currentState))
		{
			_undoStates.Push(previousState);
			_redoStates.Clear();
			UpdateToolbarState();
		}
	}

	private void Undo_Click(object sender, RoutedEventArgs e)
	{
		if (_undoStates.Count == 0)
			return;

		Dictionary<string, DeviceState> currentState = CaptureState();
		Dictionary<string, DeviceState> targetState = _undoStates.Pop();
		_redoStates.Push(currentState);
		ApplyState(targetState);
		UpdateToolbarState();
	}

	private void Redo_Click(object sender, RoutedEventArgs e)
	{
		if (_redoStates.Count == 0)
			return;

		Dictionary<string, DeviceState> currentState = CaptureState();
		Dictionary<string, DeviceState> targetState = _redoStates.Pop();
		_undoStates.Push(currentState);
		ApplyState(targetState);
		UpdateToolbarState();
	}

	private void Restore_Click(object sender, RoutedEventArgs e)
	{
		if (_initialState.Count == 0)
			return;

		Dictionary<string, DeviceState> currentState = CaptureState();

		if (AreStatesEqual(currentState, _initialState))
			return;

		_undoStates.Push(currentState);
		_redoStates.Clear();
		ApplyState(_initialState);
		UpdateToolbarState();
	}

	private Dictionary<string, DeviceState> CaptureState()
	{
		return Nodes
			.SelectMany(group => group.SubItems)
			.ToDictionary(
				item => item.PnpDeviceId,
				item => new DeviceState(item.MsiSupported, item.MsiLimit, item.MaxMsiLimit, item.DevicePolicy, item.DevicePriority, item.AssignmentSetOverride),
				StringComparer.OrdinalIgnoreCase);
	}

	private static bool AreStatesEqual(Dictionary<string, DeviceState> left, Dictionary<string, DeviceState> right)
	{
		if (left.Count != right.Count)
			return false;

		foreach ((string key, DeviceState leftState) in left)
		{
			if (!right.TryGetValue(key, out DeviceState rightState))
				return false;

			if (leftState != rightState)
				return false;
		}

		return true;
	}

	private void ApplyState(Dictionary<string, DeviceState> state)
	{
		foreach (SchedulingGroup group in Nodes)
		{
			List<DeviceInfo> devices = DeviceHelper.GetDevices(group.DeviceType);

			foreach (SchedulingItem item in group.SubItems)
			{
				if (!state.TryGetValue(item.PnpDeviceId, out DeviceState targetState))
					continue;

				DeviceInfo? deviceInfo = devices.FirstOrDefault(device => string.Equals(device.PnpDeviceId, item.PnpDeviceId, StringComparison.OrdinalIgnoreCase));
				if (deviceInfo == null)
					continue;

				bool needsApply =
					deviceInfo.MsiSupported != targetState.MsiSupported ||
					deviceInfo.MsiLimit != targetState.MsiLimit ||
					deviceInfo.DevicePolicy != targetState.DevicePolicy ||
					deviceInfo.DevicePriority != targetState.DevicePriority ||
					deviceInfo.AssignmentSetOverride != targetState.AssignmentSetOverride;

				if (needsApply)
				{
					DeviceHelper.ApplySettingsToDevices(
						[deviceInfo],
						targetState.MsiSupported == 1u,
						targetState.MsiLimit,
						targetState.DevicePolicy,
						targetState.DevicePriority,
						targetState.AssignmentSetOverride,
						item.DeviceType);
				}

				item.MsiSupported = targetState.MsiSupported;
				item.MsiLimit = targetState.MsiLimit;
				item.MaxMsiLimit = targetState.MaxMsiLimit;
				item.DevicePolicy = targetState.DevicePolicy;
				item.DevicePriority = targetState.DevicePriority;
				item.AssignmentSetOverride = targetState.AssignmentSetOverride;
			}
		}
	}

	private void UpdateToolbarState()
	{
		if (UndoButton != null)
			UndoButton.IsEnabled = _undoStates.Count > 0;

		if (RedoButton != null)
			RedoButton.IsEnabled = _redoStates.Count > 0;

		if (RestoreButton != null)
		{
			Dictionary<string, DeviceState> currentState = CaptureState();
			RestoreButton.IsEnabled = _initialState.Count > 0 && !AreStatesEqual(currentState, _initialState);
		}
	}

	private void TreeGrid_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (e.NewSize.Width <= 0 || e.NewSize.Width == e.PreviousSize.Width)
			return;

		if (sender is not SfTreeGrid treeGrid)
			return;

		foreach (TreeGridColumn column in treeGrid.Columns)
			column.Width = double.NaN;

		treeGrid.InvalidateMeasure();
		treeGrid.UpdateLayout();
	}

	private void TreeGrid_CellToolTipOpening(object sender, TreeGridCellToolTipOpeningEventArgs e)
	{
		object? record = e.Record;
		if (record is SchedulingItem item)
		{
			string? content = e.Column?.MappingName switch
			{
				nameof(SchedulingItem.Name) => item.Location,
				nameof(SchedulingItem.MsiModeDisplay) => item.MsiModeDisplay,
				nameof(SchedulingItem.MsiLimitDisplay) => item.MsiLimitDisplay,
				nameof(SchedulingItem.MaxMsiLimitDisplay) => item.MaxMsiLimitDisplay,
				nameof(SchedulingItem.DevicePolicyDisplay) => item.DevicePolicyDisplay,
				nameof(SchedulingItem.DevicePriorityDisplay) => item.DevicePriorityDisplay,
				nameof(SchedulingItem.SpecifiedProcessorsDisplay) => item.SpecifiedProcessorsDisplay,
				_ => null
			};

			if (string.IsNullOrWhiteSpace(content))
			{
				e.ToolTip.Visibility = Visibility.Collapsed;
				return;
			}

			e.ToolTip.Content = content;
			e.ToolTip.Visibility = Visibility.Visible;

			return;
		}

		if (record is SchedulingGroup group)
		{
			if (e.Column?.MappingName == nameof(SchedulingGroup.Name))
			{
				e.ToolTip.Content = $"{group.SubItems.Count} device(s)";
				e.ToolTip.Visibility = Visibility.Visible;

				return;
			}
		}

		e.ToolTip.Visibility = Visibility.Collapsed;
	}

	private void TreeGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
		=> TreeGridContextFlyoutHelper.HandleCellContextRequested<object>(sender, args, GetContextFlyoutItems, _copyTextCommand);

	private void TreeGrid_TreeGridContextFlyoutOpening(object sender, TreeGridContextFlyoutEventArgs e)
		=> TreeGridContextFlyoutHelper.ShowHeaderContextFlyout(sender, e);

	private async void TreeGrid_Tapped(object sender, TappedRoutedEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		TreeGridCell? cell = TreeGridContextFlyoutHelper.FindCell(e.OriginalSource as DependencyObject);
		if (cell?.DataContext is SchedulingItem item)
			await ShowAffinityDialog(item);
		else if (cell?.DataContext is SchedulingGroup group)
			group.IsExpanded = !group.IsExpanded;
	}

	private async void TreeGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
	{
		if (sender is not SfTreeGrid treeGrid)
			return;

		TreeGridCell? cell = TreeGridContextFlyoutHelper.FindCell(e.OriginalSource as DependencyObject);
		if (cell?.DataContext is SchedulingItem item)
			await ShowAffinityDialog(item);
	}

	private static IReadOnlyList<(string Text, string Value)> GetContextFlyoutItems(object node, string columnMappingName)
	{
		if (node is SchedulingGroup group)
		{
			if (columnMappingName == nameof(SchedulingGroup.Name))
			{
				return
				[
					("Copy Group", group.Name)
				];
			}

			return Array.Empty<(string Text, string Value)>();
		}

		if (node is SchedulingItem item)
		{
			return columnMappingName switch
			{
				nameof(SchedulingItem.Name) =>
				[
					("Copy Name", item.Name),
					("Copy PNP ID", item.PnpDeviceId),
					("Copy Location", item.Location)
				],
				nameof(SchedulingItem.MsiModeDisplay) => [("Copy MSI Mode", item.MsiModeDisplay)],
				nameof(SchedulingItem.MsiLimitDisplay) => [("Copy MSI Limit", item.MsiLimitDisplay)],
				nameof(SchedulingItem.MaxMsiLimitDisplay) => [("Copy Max MSI Limit", item.MaxMsiLimitDisplay)],
				nameof(SchedulingItem.DevicePolicyDisplay) => [("Copy Device Policy", item.DevicePolicyDisplay)],
				nameof(SchedulingItem.DevicePriorityDisplay) => [("Copy Device Priority", item.DevicePriorityDisplay)],
				nameof(SchedulingItem.SpecifiedProcessorsDisplay) => [("Copy Specified Processors", item.SpecifiedProcessorsDisplay)],
				_ => Array.Empty<(string Text, string Value)>()
			};
		}

		return Array.Empty<(string Text, string Value)>();
	}

	private async Task ShowAffinityDialog(SchedulingItem device)
	{
		Dictionary<string, DeviceState> previousState = CaptureState();

		SchedulingDialog dialog = new(device, _cpuSetsInfo)
		{
			XamlRoot = XamlRoot
		};

		dialog.Resources["ContentDialogMaxWidth"] = 1350;
		dialog.Resources["ContentDialogMaxHeight"] = 900;

		await dialog.ShowAsync();
		ApplyResult? applyResult = dialog.ApplyResult;

		if (applyResult != null)
		{
			if (applyResult.AppliedSettings.TryGetValue(device.PnpDeviceId, out DeviceInfo? updatedDevice))
			{
				device.MsiSupported = updatedDevice.MsiSupported;
				device.MsiLimit = updatedDevice.MsiLimit;
				device.MaxMsiLimit = updatedDevice.MaxMsiLimit;
				device.DevicePolicy = updatedDevice.DevicePolicy;
				device.DevicePriority = updatedDevice.DevicePriority;
				device.AssignmentSetOverride = updatedDevice.AssignmentSetOverride;
			}
			else
			{
				UpdateDevice(device.DeviceType, device.PnpDeviceId);
			}

			if (applyResult.Success)
			{
				_undoStates.Push(previousState);
				_redoStates.Clear();
				UpdateToolbarState();
			}
		}
	}
}
