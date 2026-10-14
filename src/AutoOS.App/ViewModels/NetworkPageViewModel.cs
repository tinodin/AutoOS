using System.Globalization;
using System.Windows.Input;
using AutoOS.App.Data.Commands;
using AutoOS.App.Data.Contracts.Network;
using AutoOS.App.Data.Enums;
using AutoOS.App.Data.Enums.Network;
using AutoOS.App.Data.Models.Network;
using AutoOS.App.Extensions;
using AutoOS.App.Helpers.Network;
using AutoOS.App.Services;
using AutoOS.App.ViewModels.Dialogs.Network;
using AutoOS.Core.Data.Enums.Network;
using AutoOS.Core.Data.Models.Device;
using AutoOS.Core.Data.Models.Network;
using Microsoft.UI.Xaml;

namespace AutoOS.App.ViewModels;

public sealed partial class NetworkPageViewModel(INetworkSettingsService networkService, IDialogService dialogService) : ObservableObject
{
	private readonly Stack<Dictionary<Setting, string>> _undoStates = [];
	private readonly Stack<Dictionary<Setting, string>> _redoStates = [];
	private readonly Dictionary<Setting, SettingState> _settingStates = [];
	private readonly Dictionary<Setting, Node> _changeNodes = [];
	private readonly Dictionary<DeviceInfo, Node> _changeAdapters = [];
	private readonly Dictionary<Setting, Node> _compareNodes = [];
	private readonly Dictionary<DeviceInfo, Node> _compareAdapters = [];
	public Action? RefreshFilterAction { get; set; }

	public Action? RefreshFilterOnlyAction { get; set; }

	public ObservableCollection<DeviceInfo> Adapters { get; } = [];

	public ObservableCollection<Node> TreeNodes { get; } = [];

	public ObservableCollection<Node> ChangesNodes { get; } = [];

	public ObservableCollection<Node> CompareNodes { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(UndoCommand))]
	[NotifyCanExecuteChangedFor(nameof(RedoCommand))]
	[NotifyCanExecuteChangedFor(nameof(OptimizeCommand))]
	[NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	[NotifyCanExecuteChangedFor(nameof(ToggleCompareToDefaultsCommand))]
	public partial bool IsLoaded { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SwitchPresenterValue))]
	public partial PageMode PageState { get; set; } = PageMode.Loading;

	public string SwitchPresenterValue => PageState switch
	{
		PageMode.Loading => "Loading",
		PageMode.Loaded => "Loaded",
		_ => throw new UnreachableException()
	};

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(ViewChangesVisibility))]
	public partial bool ViewChangesActive { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(NormalVisibility))]
	[NotifyPropertyChangedFor(nameof(CompareToDefaultsVisibility))]
	public partial bool CompareToDefaults { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CompareToDefaultsLabel))]
	public partial int CompareToDefaultsCount { get; set; }

	public string CompareToDefaultsLabel => $"Compare to Defaults ({CompareToDefaultsCount})";

	public bool CanToggleCompareToDefaults => IsLoaded;

	public Visibility CompareToDefaultsVisibility => CompareToDefaults ? Visibility.Visible : Visibility.Collapsed;

	public Visibility NormalVisibility => ViewChangesActive || CompareToDefaults ? Visibility.Collapsed : Visibility.Visible;

	public Visibility ViewChangesVisibility => ViewChangesActive ? Visibility.Visible : Visibility.Collapsed;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ViewChangesLabel))]
	[NotifyCanExecuteChangedFor(nameof(SaveChangesCommand))]
	public partial int ModifiedCount { get; set; }

	public string ViewChangesLabel => $"View Changes ({ModifiedCount})";

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool FilterAdapter { get; set; } = true;

	[ObservableProperty]
	public partial bool FilterDriverType { get; set; }

	[ObservableProperty]
	public partial bool FilterDriverVersion { get; set; }

	[ObservableProperty]
	public partial bool FilterSetting { get; set; } = true;

	[ObservableProperty]
	public partial bool FilterKey { get; set; } = true;

	[ObservableProperty]
	public partial bool FilterCurrent { get; set; }

	[ObservableProperty]
	public partial bool FilterRecommended { get; set; }

	[ObservableProperty]
	public partial bool FilterDefault { get; set; }

	[ObservableProperty]
	public partial FilterMode FilterMode { get; set; } = FilterMode.Contains;

	public bool CanUndo => IsLoaded && _undoStates.Count > 0;

	public bool CanRedo => IsLoaded && _redoStates.Count > 0;

	public bool CanSave => IsLoaded && ModifiedCount > 0 && _settingStates.Values.Where(state => state.IsModified).All(state => !state.HasErrors);

	public bool CanRestore => IsLoaded && _settingStates.Count > 0 && Adapters.Any(HasPendingDefaults);

	public bool CanOptimize => IsLoaded && Adapters.Any(HasPendingRecommendations);

	public ICommand CopyTextCommand { get; } = new CopyTextCommand();

	[RelayCommand(CanExecute = nameof(CanToggleCompareToDefaults))]
	private void ToggleCompareToDefaults() => RefreshFilter();

	[RelayCommand(CanExecute = nameof(CanUndo))]
	private void Undo() => MoveState(_undoStates, _redoStates);

	[RelayCommand(CanExecute = nameof(CanRedo))]
	private void Redo() => MoveState(_redoStates, _undoStates);

	[RelayCommand(CanExecute = nameof(CanOptimize))]
	private async Task OptimizeAsync()
	{
		var viewModel = new OptimizeDialogViewModel(Adapters, HasPendingRecommendations);
		if (await dialogService.ShowDialogAsync(viewModel) != DialogResult.Primary || viewModel.SelectedAdapters.Count == 0)
			return;

		Dictionary<Setting, string> previous = CaptureState();
		foreach (DeviceInfo adapter in viewModel.SelectedAdapters)
		{
			foreach (Setting setting in adapter.AdvancedSettings)
			{
				SettingState state = _settingStates[setting];
				if (string.IsNullOrEmpty(state.Recommended))
					continue;
				if (string.Equals(state.Value.Trim(), state.Recommended.Trim(), StringComparison.OrdinalIgnoreCase))
					continue;

				state.Value = state.Recommended;
			}
		}

		PushUndoState(previous);
		RefreshAfterEdit();
	}

	[RelayCommand(CanExecute = nameof(CanRestore))]
	private async Task RestoreAsync()
	{
		var viewModel = new RestoreDialogViewModel(Adapters, HasPendingDefaults);
		if (await dialogService.ShowDialogAsync(viewModel) != DialogResult.Primary || viewModel.SelectedAdapters.Count == 0)
			return;

		Dictionary<Setting, string> previous = CaptureState();
		foreach (DeviceInfo adapter in viewModel.SelectedAdapters)
		{
			foreach (Setting setting in adapter.AdvancedSettings)
				_settingStates[setting].Value = setting.Default;
		}
		PushUndoState(previous);
		RefreshAfterEdit();
	}

	[RelayCommand(CanExecute = nameof(CanSave))]
	private async Task SaveChangesAsync()
	{
		IsLoaded = false;
		try
		{
			bool allSucceeded = true;
			foreach (DeviceInfo adapter in Adapters)
			{
				var changes = adapter.AdvancedSettings.Where(setting => _settingStates[setting].IsModified)
					.ToDictionary(setting => setting, setting => _settingStates[setting].Value);
				if (changes.Count == 0)
					continue;

				if (await networkService.SaveChangesAsync(adapter, changes))
				{
					foreach ((Setting setting, string value) in changes)
					{
						setting.Value = value;
						_settingStates[setting].OriginalValue = value;
					}
				}
				else
				{
					allSucceeded = false;
				}
			}

			_undoStates.Clear();
			_redoStates.Clear();
			if (allSucceeded)
				ViewChangesActive = false;
		}
		finally
		{
			IsLoaded = true;
			RefreshAfterEdit();
		}
	}

	[RelayCommand]
	private void SetFilterMode(string mode)
	{
		if (Enum.TryParse(mode, out FilterMode filterMode))
			FilterMode = filterMode;
	}

	public async Task LoadAdaptersAsync()
	{
		PageState = PageMode.Loading;

		TreeNodes.Clear();
		ChangesNodes.Clear();
		CompareNodes.Clear();
		Adapters.Clear();
		_changeAdapters.Clear();
		_changeNodes.Clear();
		_compareNodes.Clear();
		_compareAdapters.Clear();
		_settingStates.Clear();
		_undoStates.Clear();
		_redoStates.Clear();

		IReadOnlyList<DeviceInfo> adapters = await networkService.LoadAdaptersAsync();
		int order = 0;
		foreach (DeviceInfo adapter in adapters)
		{
			Adapters.Add(adapter);
			var adapterNode = new Node(NodeKind.Adapter, adapter) { Order = order++ };
			var changesAdapter = new Node(NodeKind.Adapter, adapter) { Order = adapterNode.Order };
			_changeAdapters.Add(adapter, changesAdapter);
			_compareAdapters.Add(adapter, new Node(NodeKind.Adapter, adapter) { Order = adapterNode.Order });
			foreach (Setting setting in adapter.AdvancedSettings.OrderBy(setting => setting.Name, NetworkSettingHelper.Instance))
			{
				setting.Options.Sort((left, right) => NetworkSettingHelper.Instance.Compare(left.Name, right.Name));
				var state = new SettingState(setting);
				_settingStates.Add(setting, state);
				adapterNode.Children.Add(new Node(NodeKind.Setting, adapter, state) { Order = order++ });
				_changeNodes.Add(setting, new Node(NodeKind.Setting, adapter, state) { Order = order - 1 });
				_compareNodes.Add(setting, new Node(NodeKind.Setting, adapter, state) { Order = order - 1 });
			}
			TreeNodes.Add(adapterNode);
		}

		PageState = PageMode.Loaded;
		IsLoaded = true;
		RefreshAfterEdit();
	}

	public void BeginEdit(Node? node, string mappingName)
	{
		if (node?.State is not { } state || mappingName != nameof(Node.DisplayCurrent))
			return;

		state.EditValue = state.Value;
		state.EditOption = state.Setting.Options.FirstOrDefault(option => option.Value == state.Value);
	}

	public bool CommitEdit(Node? node, string mappingName)
	{
		if (node?.State is not { } state || mappingName != nameof(Node.DisplayCurrent))
			return false;

		string value = state.Setting.Type == NetworkSettingType.Enum
			? state.EditOption?.Value ?? state.Value
			: state.Setting.UpperCase ? state.EditValue.ToUpperInvariant() : state.EditValue;

		if (value == state.Value)
			return false;

		Dictionary<Setting, string> previous = CaptureState();
		state.Value = value;
		PushUndoState(previous);
		return true;
	}

	public static IReadOnlyList<(string Text, string Value)> GetContextFlyoutItems(Node node, string columnMappingName)
	{
		if (columnMappingName == nameof(Node.DisplayName))
		{
			if (node.NodeKind == NodeKind.Adapter)
			{
				return new[]
				{
					("Copy Adapter", node.BaseDisplayName),
					("Copy Driver Type", node.Adapter.DriverType.ToString()),
					("Copy Driver Version", node.Adapter.CurrentVersion)
				};
			}

			return new[]
			{
				("Copy Setting", node.BaseDisplayName),
				("Copy Key", node.Description)
			};
		}

		if (node.Setting is not { } setting)
			return Array.Empty<(string Text, string Value)>();

		string displayValue = columnMappingName switch
		{
			nameof(Node.DisplayOriginal) => node.DisplayOriginal,
			nameof(Node.DisplayRecommended) => node.DisplayRecommended,
			nameof(Node.DisplayDefault) => node.DisplayDefault,
			_ => node.DisplayCurrent
		};

		if (setting.Options.Count > 0)
		{
			return new[]
			{
				("Copy Value", displayValue)
			};
		}
		else
		{
			return new[]
			{
				("Copy Value", displayValue),
				("Copy Minimum", setting.Min?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
				("Copy Maximum", setting.Max?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
				("Copy Increment", setting.Step?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)
			};
		}
	}

	public void RefreshAfterEdit()
	{
		ModifiedCount = _settingStates.Values.Count(state => state.IsModified);
		UndoCommand.NotifyCanExecuteChanged();
		RedoCommand.NotifyCanExecuteChanged();
		OptimizeCommand.NotifyCanExecuteChanged();
		RestoreCommand.NotifyCanExecuteChanged();
		SaveChangesCommand.NotifyCanExecuteChanged();
		CompareToDefaultsCount = _compareNodes.Values.Count(node => !node.IsDefault);
		SyncTree(ChangesNodes, _changeAdapters, _changeNodes, static node => node.IsModified);
		SyncTree(CompareNodes, _compareAdapters, _compareNodes, static node => !node.IsDefault);

		UpdateNodeCounts();
		RefreshFilterOnlyAction?.Invoke();
	}

	public void UpdateNodeCounts()
	{
		foreach (Node adapter in TreeNodes.Concat(CompareNodes).Concat(ChangesNodes))
			adapter.DisplayName = $"{adapter.BaseDisplayName} ({adapter.Children.Count(MatchesFilter)})";
	}

	public void RefreshFilter() => RefreshFilterAction?.Invoke();

	public bool MatchesFilter(object item)
	{
		if (item is not Node node)
			return true;
		if (node.NodeKind == NodeKind.Adapter)
			return node.Children.Any(MatchesFilter);

		if (CompareToDefaults && node.IsDefault)
			return false;

		string query = SearchText.Trim();
		return query.Length == 0
			|| (FilterAdapter && TextMatches(node.Adapter.FriendlyName, query))
			|| (FilterDriverType && TextMatches(node.Adapter.DriverType.ToString(), query))
			|| (FilterDriverVersion && TextMatches(node.Adapter.CurrentVersion, query))
			|| (FilterSetting && TextMatches(node.BaseDisplayName, query))
			|| (FilterKey && TextMatches(node.Description, query))
			|| (FilterCurrent && (TextMatches(node.DisplayCurrent, query) || (ViewChangesActive && TextMatches(node.DisplayOriginal, query))))
			|| (FilterRecommended && TextMatches(node.DisplayRecommended, query))
			|| (FilterDefault && TextMatches(node.DisplayDefault, query));
	}

	partial void OnSearchTextChanged(string value) => RefreshFilter();

	partial void OnFilterAdapterChanged(bool value) => RefreshFilter();

	partial void OnFilterDriverTypeChanged(bool value) => RefreshFilter();

	partial void OnFilterDriverVersionChanged(bool value) => RefreshFilter();

	partial void OnFilterSettingChanged(bool value) => RefreshFilter();

	partial void OnFilterKeyChanged(bool value) => RefreshFilter();

	partial void OnFilterCurrentChanged(bool value) => RefreshFilter();

	partial void OnFilterRecommendedChanged(bool value) => RefreshFilter();

	partial void OnFilterDefaultChanged(bool value) => RefreshFilter();

	partial void OnFilterModeChanged(FilterMode value) => RefreshFilter();

	partial void OnCompareToDefaultsChanged(bool value)
	{
		if (value)
			ViewChangesActive = false;
		RefreshFilter();
	}

	partial void OnViewChangesActiveChanged(bool value)
	{
		if (value)
			CompareToDefaults = false;
		RefreshFilter();
	}

	private bool HasPendingRecommendations(DeviceInfo adapter) => adapter.AdvancedSettings.Any(setting => _settingStates[setting].HasPendingRecommendation);

	private bool HasPendingDefaults(DeviceInfo adapter) => adapter.AdvancedSettings.Any(setting => !_settingStates[setting].IsDefault);

	private bool TextMatches(string text, string query) => FilterMode == FilterMode.ExactMatch
		? text.Equals(query, StringComparison.OrdinalIgnoreCase)
		: text.Contains(query, StringComparison.OrdinalIgnoreCase);

	private void MoveState(Stack<Dictionary<Setting, string>> from, Stack<Dictionary<Setting, string>> to)
	{
		if (from.Count == 0)
			return;

		to.Push(CaptureState());
		foreach ((Setting setting, string value) in from.Pop())
			_settingStates[setting].Value = value;
		RefreshAfterEdit();
	}

	private static void SyncTree(ObservableCollection<Node> collection, Dictionary<DeviceInfo, Node> adapters, Dictionary<Setting, Node> nodes, Func<Node, bool> include)
	{
		foreach ((DeviceInfo adapter, Node parent) in adapters)
		{
			foreach (Setting setting in adapter.AdvancedSettings)
			{
				Node node = nodes[setting];
				bool included = include(node);
				bool present = parent.Children.Contains(node);
				if (included && !present)
					parent.Children.InsertOrdered(node);
				else if (!included && present)
					parent.Children.Remove(node);
			}

			if (parent.Children.Count > 0 && !collection.Contains(parent))
				collection.InsertOrdered(parent);
			else if (parent.Children.Count == 0)
				collection.Remove(parent);
		}
	}

	private Dictionary<Setting, string> CaptureState() => _settingStates.ToDictionary(pair => pair.Key, pair => pair.Value.Value);

	private void PushUndoState(Dictionary<Setting, string> previous)
	{
		if (previous.All(pair => _settingStates[pair.Key].Value == pair.Value))
			return;

		_undoStates.Push(previous);
		_redoStates.Clear();
	}
}
