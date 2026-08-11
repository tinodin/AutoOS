using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using AutoOS.Core.Helpers.Games;
using AutoOS.Core.Helpers.Games.Models;
using AutoOS.Core.Helpers.Processes;
using AutoOS.Core.Helpers.Services;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using ValveKeyValue;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using WinRT;

namespace AutoOS.App.Views.Settings.Games;

[TemplatePart(Name = nameof(PART_BackDropImage), Type = typeof(AnimatedImage))]
[TemplatePart(Name = nameof(PART_ScrollViewer), Type = typeof(ScrollViewer))]
[TemplatePart(Name = nameof(PART_ItemsRepeater), Type = typeof(ItemsRepeater))]

public partial class HeaderCarousel : ItemsControl
{
	private const string PART_ScrollViewer = "PART_ScrollViewer";
	private const string PART_BackDropImage = "PART_BackDropImage";
	private const string PART_ItemsRepeater = "PART_ItemsRepeater";
	private ScrollViewer scrollViewer = null!;
	private AnimatedImage backDropImage = null!;
	private ItemsRepeater itemsRepeater = null!;

	private TextBlock PageTitle = null!;
	private SwitchPresenter SwitchPresenter = null!;
	//private TextBlock SwitchPresenter_TextBlock;

	private StackPanel NoGames_StackPanel = null!;

	private Grid MetadataGrid = null!;
	private ScrollViewer Metadata_ScrollViewer = null!;

	private Card Screenshots_Card = null!;
	//private GameGallery Screenshots_Gallery;
	//private ScrollViewer Videos_ScrollViewer;

	private Button Play = null!;
	private Button Update = null!;
	private Button StopProcesses = null!;
	private Button RestartProcesses = null!;

	private bool isInitializingEpicGamesAccounts = true;
	private bool isInitializingSteamAccounts = true;
	private Button EpicGamesButton = null!;
	private ComboBox EpicGamesAccounts = null!;
	private Button AddEpicGamesAccount = null!;
	private Button RemoveEpicGamesAccount = null!;

	private Button SteamButton = null!;
	private ComboBox SteamAccounts = null!;
	private Button AddSteamAccount = null!;
	private Button RemoveSteamAccount = null!;

	private StackPanel EpicGrowl = null!;
	private StackPanel SteamGrowl = null!;

	private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

	private TextBlock AgeRatingDescriptionText = null!;
	private TextBlock ElementsText = null!;

	private AutoSuggestBox SearchBox = null!;
	private Button Sort = null!;
	private string currentSortKey = "Title";
	private bool ascending = true;

	private RadioMenuFlyoutItem SortByName = null!;
	private RadioMenuFlyoutItem SortByLauncher = null!;
	private RadioMenuFlyoutItem SortByRating = null!;
	private RadioMenuFlyoutItem SortByTimePlayed = null!;
	private RadioMenuFlyoutItem SortByRecentlyPlayed = null!;
	private RadioMenuFlyoutItem SortAscending = null!;
	private RadioMenuFlyoutItem SortDescending = null!;

	//public event EventHandler<HeaderCarouselEventArgs> ItemClick;

	private readonly Random random = new();
	private readonly DispatcherTimer selectionTimer = new();
	private readonly DispatcherTimer deselectionTimer = new();
	private readonly List<int> numbers = [];
	private HeaderCarouselItem? selectedTile;
	private int currentIndex;

	private BlurEffectManager? _blurManager;

	private static InputInjector? _inputInjector;
	private static DispatcherTimer? _gamepadPollingTimer;
	private static GamepadButtons _lastButtons = GamepadButtons.None;
	private static bool _lastHorizontalState = false;
	private const double ThumbstickThreshold = 0.5;

	private static readonly Dictionary<GamepadButtons, DateTimeOffset> _buttonPressStartTimes = [];
	private static readonly Dictionary<GamepadButtons, DateTimeOffset> _lastButtonRepeatTimes = [];
	private static readonly TimeSpan _initialRepeatDelay = TimeSpan.FromMilliseconds(500);
	private static readonly TimeSpan _subsequentRepeatDelay = TimeSpan.FromMilliseconds(100);
	private static readonly List<HeaderCarousel> _activeInstances = [];
	private static bool _staticEventsSubscribed = false;
	private static bool _windowIsActive = true;
	private static DependencyObject? _lastFocusedElement;
	private static DateTimeOffset _bottomFocusTime;
	private static GamepadButtons _scrollingButtons = GamepadButtons.None;
	public ObservableCollection<InfoItem> InfoItems { get; } = [];

	public HeaderCarousel()
	{
		DefaultStyleKey = typeof(HeaderCarousel);
	}

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		selectionTimer.Interval = SelectionDuration;
		deselectionTimer.Interval = DeSelectionDuration;

		scrollViewer = (ScrollViewer)GetTemplateChild(PART_ScrollViewer)!;
		backDropImage = (AnimatedImage)GetTemplateChild(PART_BackDropImage)!;
		itemsRepeater = (ItemsRepeater)GetTemplateChild("PART_ItemsRepeater")!;
		itemsRepeater.ItemsSource = InfoItems;
		itemsRepeater.ElementPrepared += ItemsRepeater_ElementPrepared;

		PageTitle = (TextBlock)GetTemplateChild("PageTitle")!;

		SearchBox = (AutoSuggestBox)GetTemplateChild("SearchBox")!;
		SearchBox.TextChanged += SearchBox_TextChanged;
		SearchBox.QuerySubmitted += SearchBox_QuerySubmitted;

		Sort = (Button)GetTemplateChild("Sort")!;

		SortByName = (RadioMenuFlyoutItem)GetTemplateChild("SortByName")!;
		SortByLauncher = (RadioMenuFlyoutItem)GetTemplateChild("SortByLauncher")!;
		SortByRating = (RadioMenuFlyoutItem)GetTemplateChild("SortByRating")!;
		SortByTimePlayed = (RadioMenuFlyoutItem)GetTemplateChild("SortByTimePlayed")!;
		SortByRecentlyPlayed = (RadioMenuFlyoutItem)GetTemplateChild("SortByRecentlyPlayed")!;
		SortAscending = (RadioMenuFlyoutItem)GetTemplateChild("SortAscending")!;
		SortDescending = (RadioMenuFlyoutItem)GetTemplateChild("SortDescending")!;

		SortByName.Click += SortKey_Click;
		SortByLauncher.Click += SortKey_Click;
		SortByRating.Click += SortKey_Click;
		SortByTimePlayed.Click += SortKey_Click;
		SortByRecentlyPlayed.Click += SortKey_Click;
		SortAscending.Click += SortOrder_Click;
		SortDescending.Click += SortOrder_Click;

		EpicGamesButton = (Button)GetTemplateChild("EpicGamesButton")!;
		EpicGamesAccounts = (ComboBox)GetTemplateChild("EpicGamesAccounts")!;
		EpicGamesAccounts.SelectionChanged += EpicGamesAccounts_SelectionChanged;
		AddEpicGamesAccount = (Button)GetTemplateChild("AddEpicGamesAccount")!;
		AddEpicGamesAccount.Click += AddEpicGamesAccount_Click;
		RemoveEpicGamesAccount = (Button)GetTemplateChild("RemoveEpicGamesAccount")!;
		RemoveEpicGamesAccount.Click += RemoveEpicGamesAccount_Click;
		EpicGrowl = (StackPanel)GetTemplateChild("EpicGrowl")!;
		Growl.Register("Epic", EpicGrowl);
		LoadEpicGamesAccounts();

		SteamButton = (Button)GetTemplateChild("SteamButton")!;
		SteamAccounts = (ComboBox)GetTemplateChild("SteamAccounts")!;
		SteamAccounts.SelectionChanged += SteamAccounts_SelectionChanged;
		AddSteamAccount = (Button)GetTemplateChild("AddSteamAccount")!;
		AddSteamAccount.Click += AddSteamAccount_Click;
		RemoveSteamAccount = (Button)GetTemplateChild("RemoveSteamAccount")!;
		RemoveSteamAccount.Click += RemoveSteamAccount_Click;
		SteamGrowl = (StackPanel)GetTemplateChild("SteamGrowl")!;
		Growl.Register("Steam", SteamGrowl);
		LoadSteamAccounts();

		SwitchPresenter = (SwitchPresenter)GetTemplateChild("SwitchPresenter")!;
		//SwitchPresenter_TextBlock = GetTemplateChild("SwitchPresenter_TextBlock") as TextBlock;
		NoGames_StackPanel = (StackPanel)GetTemplateChild("NoGames_StackPanel")!;
		MetadataGrid = (Grid)GetTemplateChild("MetadataGrid")!;
		Metadata_ScrollViewer = (ScrollViewer)GetTemplateChild("Metadata_ScrollViewer")!;

		Play = (Button)GetTemplateChild("Play")!;
		Play.Click += Play_Click;
		Play.KeyDown += BottomButtons_KeyDown;
		Update = (Button)GetTemplateChild("Update")!;
		Update.Click += Update_Click;
		Update.KeyDown += BottomButtons_KeyDown;
		StopProcesses = (Button)GetTemplateChild("StopProcesses")!;
		StopProcesses.Click += StopProcesses_Click;
		StopProcesses.KeyDown += BottomButtons_KeyDown;
		RestartProcesses = (Button)GetTemplateChild("RestartProcesses")!;
		RestartProcesses.Click += RestartProcesses_Click;
		RestartProcesses.KeyDown += BottomButtons_KeyDown;

		AgeRatingDescriptionText = (TextBlock)GetTemplateChild("AgeRatingDescriptionText")!;
		ElementsText = (TextBlock)GetTemplateChild("ElementsText")!;

		Screenshots_Card = (Card)GetTemplateChild("Screenshots_Card")!;
		//Screenshots_Gallery = GetTemplateChild("Screenshots_Gallery") as GameGallery;
		//Videos_ScrollViewer = GetTemplateChild("Videos_ScrollViewer") as ScrollViewer;

		Loaded -= HeaderCarousel_Loaded;
		Loaded += HeaderCarousel_Loaded;
		Unloaded -= HeaderCarousel_Unloaded;
		Unloaded += HeaderCarousel_Unloaded;

		LoadGames();

		if (backDropImage != null)
		{
			_blurManager = new BlurEffectManager(backDropImage);

			ApplyBackdropBlur();
		}

		InitializeGamepadSupport();
	}

	private void InitializeGamepadSupport()
	{
		if (_staticEventsSubscribed)
			return;
		_staticEventsSubscribed = true;

		_inputInjector = InputInjector.TryCreate();

		Gamepad.GamepadAdded += (s, e) =>
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				if (_windowIsActive)
					_gamepadPollingTimer?.Start();
			});
		};

		_gamepadPollingTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(8)
		};
		_gamepadPollingTimer.Tick += GamepadPollingTimer_Tick;

		if (Gamepad.Gamepads.Count > 0)
		{
			_gamepadPollingTimer.Start();
		}

		if (MainWindow.Instance != null)
		{
			MainWindow.Instance.Activated += (s, e) =>
			{
				_windowIsActive = e.WindowActivationState != WindowActivationState.Deactivated;
				if (_windowIsActive)
				{
					if (Gamepad.Gamepads.Count > 0)
						_gamepadPollingTimer?.Start();
				}
				else
				{
					_gamepadPollingTimer?.Stop();
				}
			};
		}
	}

	private static void GamepadPollingTimer_Tick(object? sender, object e)
	{
		HeaderCarousel? activeInstance = _activeInstances.FirstOrDefault();
		if (activeInstance?.XamlRoot is not XamlRoot root)
			return;

		IReadOnlyList<Gamepad> gamepads = Gamepad.Gamepads;
		if (gamepads.Count == 0)
			return;

		GamepadReading reading = gamepads[0].GetCurrentReading();
		GamepadButtons buttons = reading.Buttons;

		if (reading.LeftThumbstickX < -ThumbstickThreshold)
			buttons |= GamepadButtons.DPadLeft;
		if (reading.LeftThumbstickX > ThumbstickThreshold)
			buttons |= GamepadButtons.DPadRight;
		if (reading.LeftThumbstickY < -ThumbstickThreshold)
			buttons |= GamepadButtons.DPadDown;
		if (reading.LeftThumbstickY > ThumbstickThreshold)
			buttons |= GamepadButtons.DPadUp;

		var currentFocused = FocusManager.GetFocusedElement(root) as DependencyObject;

		if ((buttons & ~_lastButtons) != GamepadButtons.None &&
			VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Count == 0 &&
			activeInstance.selectedTile != null &&
			!IsElementDescendantOf(currentFocused, activeInstance))
		{
			int idx = activeInstance.Items.IndexOf(activeInstance.selectedTile);
			if (idx >= 0)
			{
				(activeInstance.ContainerFromIndex(idx) as Control)?.Focus(FocusState.Programmatic);
				currentFocused = FocusManager.GetFocusedElement(root) as DependencyObject;
			}
		}

		if ((activeInstance.Play != null && !activeInstance.Play.IsEnabled) &&
			(currentFocused == activeInstance.Play || (_lastFocusedElement == activeInstance.Play && (currentFocused == null || !IsElementDescendantOf(currentFocused, activeInstance)))))
		{
			if (activeInstance.selectedTile != null)
			{
				int idx = activeInstance.Items.IndexOf(activeInstance.selectedTile);
				if (idx >= 0)
				{
					(activeInstance.ContainerFromIndex(idx) as Control)?.Focus(FocusState.Programmatic);
					currentFocused = FocusManager.GetFocusedElement(root) as DependencyObject;
				}
			}
			_lastFocusedElement = currentFocused;
		}

		if (root.Content != null && !IsElementDescendantOf(currentFocused, activeInstance))
		{
			if (_lastFocusedElement != null && IsElementDescendantOf(_lastFocusedElement, activeInstance.Metadata_ScrollViewer))
			{
				(_lastFocusedElement as Control)?.Focus(FocusState.Programmatic);
				currentFocused = _lastFocusedElement;
			}
		}

		if (currentFocused == null)
		{
			_lastButtons = buttons;
			return;
		}

		if (currentFocused != _lastFocusedElement)
		{
			_lastFocusedElement = currentFocused;
			_bottomFocusTime = DateTimeOffset.Now;
		}

		bool isLeft = buttons.HasFlag(GamepadButtons.DPadLeft);
		bool isRight = buttons.HasFlag(GamepadButtons.DPadRight);
		bool suppressHorizontal = false;

		if (currentFocused is HeaderCarouselItem focusedItem && activeInstance.Items.Contains(focusedItem))
		{
			int index = activeInstance.Items.IndexOf(focusedItem);
			suppressHorizontal = (isLeft && index == 0) || (isRight && index == activeInstance.Items.Count - 1);
		}
		else if (isLeft || isRight)
		{
			if (isLeft && (currentFocused == activeInstance.Play ||
			currentFocused == activeInstance.StopProcesses ||
				IsElementDescendantOf(currentFocused, activeInstance.Metadata_ScrollViewer) ||
				IsElementDescendantOf(currentFocused, activeInstance.SearchBox)))
			{
				suppressHorizontal = true;
				_lastHorizontalState = true;
			}
			else if (!_lastHorizontalState)
			{
				FocusNavigationDirection direction = isLeft ? FocusNavigationDirection.Left : FocusNavigationDirection.Right;
				var next = FocusManager.FindNextElement(direction, new FindNextElementOptions { SearchRoot = root.Content }) as UIElement;

				if (next != null && currentFocused is UIElement currentUI)
				{
					try
					{
						double currentY = currentUI.TransformToVisual(root.Content).TransformPoint(new Point(0, 0)).Y;
						double nextY = next.TransformToVisual(root.Content).TransformPoint(new Point(0, 0)).Y;
						if (Math.Abs(currentY - nextY) > 50)
							next = null;
					}
					catch { }
				}

				if (next == null || next == currentFocused || next is HeaderCarouselItem)
				{
					suppressHorizontal = true;
					_lastHorizontalState = true;
				}
			}
		}

		_lastHorizontalState = isLeft || isRight;

		_scrollingButtons &= buttons;

		bool isBottomButtonFocused = currentFocused is Button btn && (
			btn == activeInstance.Play ||
			btn == activeInstance.Update ||
			btn == activeInstance.StopProcesses ||
			btn == activeInstance.RestartProcesses);

		if (isBottomButtonFocused)
		{
			double speed = 0;
			if (reading.LeftThumbstickY < -0.2 || buttons.HasFlag(GamepadButtons.DPadDown))
			{
				speed = reading.LeftThumbstickY < -0.2 ? -reading.LeftThumbstickY * 15.0 : 15.0;
			}
			else if (reading.LeftThumbstickY > 0.2 || buttons.HasFlag(GamepadButtons.DPadUp))
			{
				speed = reading.LeftThumbstickY > 0.2 ? -reading.LeftThumbstickY * 15.0 : -15.0;
			}

			if (speed != 0 && activeInstance.Metadata_ScrollViewer is ScrollViewer sv && (DateTimeOffset.Now - _bottomFocusTime).TotalMilliseconds > 300)
			{
				if ((speed > 0 && sv.VerticalOffset < sv.ScrollableHeight) || (speed < 0 && sv.VerticalOffset > 0))
				{
					sv.ChangeView(null, Math.Max(0, sv.VerticalOffset + speed), null, true);
					_scrollingButtons |= (speed > 0 ? GamepadButtons.DPadDown : GamepadButtons.DPadUp);
				}
			}
		}

		CheckAndInject(buttons, GamepadButtons.DPadLeft, VirtualKey.GamepadDPadLeft, suppressHorizontal);
		CheckAndInject(buttons, GamepadButtons.DPadRight, VirtualKey.GamepadDPadRight, suppressHorizontal);

		bool suppressUp = _scrollingButtons.HasFlag(GamepadButtons.DPadUp);
		if (buttons.HasFlag(GamepadButtons.DPadUp) && !suppressUp)
		{
			if (IsElementDescendantOf(currentFocused, activeInstance.SearchBox) ||
				currentFocused == activeInstance.Sort ||
				currentFocused == activeInstance.EpicGamesButton ||
				currentFocused == activeInstance.SteamButton)
			{
				suppressUp = true;
			}
			else if (IsElementDescendantOf(currentFocused, activeInstance.Metadata_ScrollViewer))
			{
				if (activeInstance.Metadata_ScrollViewer.VerticalOffset <= 2 && !_lastButtons.HasFlag(GamepadButtons.DPadUp))
				{
					if (activeInstance.Play?.IsEnabled == true)
						activeInstance.Play.Focus(FocusState.Programmatic);
					else if (activeInstance.StopProcesses?.Visibility == Visibility.Visible && activeInstance.StopProcesses.IsEnabled)
						activeInstance.StopProcesses.Focus(FocusState.Programmatic);
					else if (activeInstance.Update?.Visibility == Visibility.Visible && activeInstance.Update.IsEnabled)
						activeInstance.Update.Focus(FocusState.Programmatic);

					suppressUp = true;
				}
			}
		}
		CheckAndInject(buttons, GamepadButtons.DPadUp, VirtualKey.GamepadDPadUp, suppressUp);

		bool suppressDown = _scrollingButtons.HasFlag(GamepadButtons.DPadDown);
		if (currentFocused is HeaderCarouselItem && buttons.HasFlag(GamepadButtons.DPadDown) && !_lastButtons.HasFlag(GamepadButtons.DPadDown))
		{
			if (activeInstance.Play == null || !activeInstance.Play.IsEnabled)
			{
				if (activeInstance.StopProcesses?.Visibility == Visibility.Visible && activeInstance.StopProcesses.IsEnabled)
				{
					activeInstance.StopProcesses.Focus(FocusState.Programmatic);
					suppressDown = true;
				}
				else if (activeInstance.Metadata_ScrollViewer != null)
				{
					activeInstance.Metadata_ScrollViewer.Focus(FocusState.Programmatic);
					suppressDown = true;
				}
			}
		}
		CheckAndInject(buttons, GamepadButtons.DPadDown, VirtualKey.GamepadDPadDown, suppressDown);

		if (currentFocused is HeaderCarouselItem)
		{
			if (buttons.HasFlag(GamepadButtons.A) && !_lastButtons.HasFlag(GamepadButtons.A))
			{
				if (activeInstance.Play?.IsEnabled == true)
					activeInstance.Play.Focus(FocusState.Programmatic);
				else if (activeInstance.StopProcesses?.Visibility == Visibility.Visible && activeInstance.StopProcesses.IsEnabled)
					activeInstance.StopProcesses.Focus(FocusState.Programmatic);
				else
					activeInstance.Metadata_ScrollViewer?.Focus(FocusState.Programmatic);
			}
		}
		else
		{
			CheckAndInject(buttons, GamepadButtons.A, VirtualKey.GamepadA);
		}

		if (isBottomButtonFocused && buttons.HasFlag(GamepadButtons.B) && !_lastButtons.HasFlag(GamepadButtons.B))
		{
			if (activeInstance.selectedTile != null)
			{
				int idx = activeInstance.Items.IndexOf(activeInstance.selectedTile);
				if (idx >= 0)
					(activeInstance.ContainerFromIndex(idx) as Control)?.Focus(FocusState.Programmatic);
			}
		}
		else
		{
			CheckAndInject(buttons, GamepadButtons.B, VirtualKey.GamepadB);
		}

		CheckAndInject(buttons, GamepadButtons.X, VirtualKey.GamepadX);
		CheckAndInject(buttons, GamepadButtons.Y, VirtualKey.GamepadY);

		if (IsElementDescendantOf(currentFocused, activeInstance))
		{
			if (buttons.HasFlag(GamepadButtons.LeftShoulder) && !_lastButtons.HasFlag(GamepadButtons.LeftShoulder))
				activeInstance.JumpToStart();
			else if (buttons.HasFlag(GamepadButtons.RightShoulder) && !_lastButtons.HasFlag(GamepadButtons.RightShoulder))
				activeInstance.JumpToEnd();
		}

		_lastButtons = buttons;
	}

	private static bool IsElementDescendantOf(DependencyObject? element, DependencyObject parent)
	{
		while (element != null)
		{
			if (element == parent)
				return true;
			element = VisualTreeHelper.GetParent(element);
		}
		return false;
	}

	private static void CheckAndInject(GamepadButtons current, GamepadButtons target, VirtualKey key, bool suppressInjection = false)
	{
		if ((current & target) != 0)
		{
			DateTimeOffset now = DateTimeOffset.Now;
			if ((_lastButtons & target) == 0)
			{
				_buttonPressStartTimes[target] = now;
				_lastButtonRepeatTimes[target] = now;
				if (!suppressInjection)
					InjectKey(key);
			}
			else
			{
				if (_buttonPressStartTimes.TryGetValue(target, out DateTimeOffset startTime))
				{
					TimeSpan holdDuration = now - startTime;
					if (holdDuration >= _initialRepeatDelay)
					{
						if (_lastButtonRepeatTimes.TryGetValue(target, out DateTimeOffset lastRepeat) && (now - lastRepeat) >= _subsequentRepeatDelay)
						{
							_lastButtonRepeatTimes[target] = now;
							if (!suppressInjection)
								InjectKey(key);
						}
					}
				}
			}
		}
		else
		{
			_buttonPressStartTimes.Remove(target);
			_lastButtonRepeatTimes.Remove(target);
		}
	}

	private static void InjectKey(VirtualKey key)
	{
		if (_inputInjector == null)
			return;

		var info = new InjectedInputKeyboardInfo
		{
			VirtualKey = (ushort)key,
			KeyOptions = InjectedInputKeyOptions.None
		};
		_inputInjector.InjectKeyboardInput(new[] { info });

		info.KeyOptions = InjectedInputKeyOptions.KeyUp;
		_inputInjector.InjectKeyboardInput(new[] { info });
	}

	public void JumpToStart()
	{
		if (Items.Count > 0 && Items[0] is HeaderCarouselItem tile)
		{
			tile.Focus(FocusState.Programmatic);
		}
	}

	public void JumpToEnd()
	{
		if (Items.Count > 0 && Items[^1] is HeaderCarouselItem tile)
		{
			tile.Focus(FocusState.Programmatic);
		}
	}

	private void BottomButtons_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.GamepadDPadDown || e.Key == VirtualKey.Down)
		{
			Metadata_ScrollViewer.ChangeView(null, Metadata_ScrollViewer.VerticalOffset + 100, null);
			e.Handled = true;
		}
		else if (e.Key == VirtualKey.GamepadDPadUp || e.Key == VirtualKey.Up)
		{
			if (Metadata_ScrollViewer.VerticalOffset > 0)
			{
				Metadata_ScrollViewer.ChangeView(null, Math.Max(0, Metadata_ScrollViewer.VerticalOffset - 100), null);
				e.Handled = true;
			}
		}
	}

	private async void LoadGames()
	{
		// reset
		LoadSortSettings();

		// load games
		var tasks = new List<Task<List<GameModel>>>();

		if (EpicGamesAccounts.SelectedItem is ComboBoxItem item && item.Content?.ToString() != "Not logged in" && EpicGamesButton.Visibility == Visibility.Visible)
		{
			tasks.Add(EpicGamesHelper.GetGames());
		}

		if ((SteamAccounts.SelectedItem is string && SteamAccounts.SelectedItem?.ToString() != "Not logged in") && SteamButton.Visibility == Visibility.Visible)
		{
			tasks.Add(SteamHelper.GetGames());
		}

		switch (localSettings.Values["SwitchEmulator"] as string ?? "Eden")
		{
			case "Eden":
				tasks.Add(EdenHelper.GetGames(localSettings.Values["EdenLocation"]?.ToString() ?? "", localSettings.Values["EdenDataLocation"]?.ToString() ?? ""));
				break;

			case "Citron":
				tasks.Add(CitronHelper.GetGames(localSettings.Values["CitronLocation"]?.ToString() ?? "", localSettings.Values["CitronDataLocation"]?.ToString() ?? ""));
				break;

			case "Ryujinx":
				tasks.Add(RyujinxHelper.GetGames(localSettings.Values["RyujinxLocation"]?.ToString() ?? "", localSettings.Values["RyujinxDataLocation"]?.ToString() ?? ""));
				break;
		}

		var exceptions = new List<Exception>();

		try
		{
			await Task.WhenAll(tasks);
		}
		catch { }

		Items.Clear();

		foreach (Task<List<GameModel>> task in tasks)
		{
			if (task.Status == TaskStatus.RanToCompletion && task.Result != null)
			{
				AddGames(task.Result);
			}
			else if (task.IsFaulted && task.Exception != null)
			{
				exceptions.Add(task.Exception.InnerException ?? task.Exception);
			}
		}

		foreach (Exception exception in exceptions)
		{
			DispatcherQueue.TryEnqueue(() => { throw exception; });
		}

		// sort games
		LoadSortSettings();

		// show games status
		NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

		// show games
		SwitchPresenter.Value = false;

		await Task.Delay(700);

		if (Items.Count == 0)
			return;

		if (Items[0] is HeaderCarouselItem tile)
		{
			selectedTile = tile;
			SelectTile(true);
			(ContainerFromIndex(0) as Control)?.Focus(FocusState.Programmatic);
			PageTitle.RequestedTheme = ElementTheme.Dark;

			if (localSettings.Values["GamepadControlsShown"] is not true)
			{
				localSettings.Values["GamepadControlsShown"] = true;
				_ = ShowGamepadControlsDialog(this);
			}
		}

		if (Items.Count > 1)
			selectionTimer.Start();
	}

	private void AddGames(List<GameModel> games)
	{
		foreach (GameModel game in games)
		{
			Items.Add(new HeaderCarouselItem
			{
				Launcher = game.Launcher ?? "",
				LauncherLocation = game.LauncherLocation ?? "",
				DataLocation = game.DataLocation ?? "",
				CatalogNamespace = game.CatalogNamespace ?? "",
				CatalogItemId = game.CatalogItemId ?? "",
				AppName = game.AppName ?? "",
				InstallLocation = game.InstallLocation ?? "",
				LaunchCommand = game.LaunchCommand ?? "",
				LaunchExecutable = game.LaunchExecutable ?? "",
				GameLocation = game.GameLocation ?? "",
				GameID = game.GameID ?? "",
				ProcessNames = game.ProcessNames,
				ArtifactId = game.ArtifactId ?? "",
				UpdateIsAvailable = game.UpdateIsAvailable,
				ImageUrl = game.ImageUrl ?? "",
				BackgroundImageUrl = game.BackgroundImageUrl ?? "",
				Title = game.Title ?? "",
				Developers = game.Developers ?? "",
				Genres = game.Genres ?? [],
				Features = game.Features ?? [],
				Rating = game.Rating,
				PlayTime = game.PlayTime ?? "",
				AgeRatingUrl = game.AgeRatingUrl ?? "",
				AgeRatingTitle = game.AgeRatingTitle ?? "",
				AgeRatingDescription = game.AgeRatingDescription ?? "",
				Elements = game.Elements ?? "",
				Description = game.Description ?? "",
				Screenshots = game.Screenshots,
				ReleaseDate = game.ReleaseDate ?? "",
				Size = game.Size ?? "",
				Version = game.Version ?? "",
				Width = 240,
				Height = 320
			});
		}
	}

	private void ApplyBackdropBlur()
	{
		if (_blurManager == null)
			return;

		if (IsBlurEnabled)
		{
			_blurManager.BlurAmount = BlurAmount;

			_blurManager.EnableBlur();
		}
		else
		{
			_blurManager.DisableBlur();
		}
	}

	private void HeaderCarousel_Unloaded(object sender, RoutedEventArgs e)
	{
		_activeInstances.Remove(this);

		UnsubscribeToEvents();

		ElementSoundPlayer.State = ElementSoundPlayerState.Off;

		if (!string.IsNullOrEmpty(ArtifactId) && epicGameStartTimes.TryGetValue(ArtifactId, out DateTime startTime))
		{
			EpicGamesHelper.AddPlaytime(ArtifactId, startTime);
			epicGameStartTimes.Remove(ArtifactId);
		}
	}

	private async void HeaderCarousel_Loaded(object sender, RoutedEventArgs e)
	{
		if (!_activeInstances.Contains(this))
			_activeInstances.Add(this);

		if (IsAutoScrollEnabled)
			selectionTimer.Tick += SelectionTimer_Tick;

		ElementSoundPlayer.State = ElementSoundPlayerState.On;
	}

	private async Task ShowGamepadControlsDialog(HeaderCarousel activeInstance)
	{
		static StackPanel CreateRow(string[] glyphs, string description)
		{
			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
			var iconContainer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };

			for (int i = 0; i < glyphs.Length; i++)
			{
				iconContainer.Children.Add(new FontIcon
				{
					Glyph = glyphs[i],
				});

				if (i < glyphs.Length - 1)
				{
					iconContainer.Children.Add(new TextBlock
					{
						Text = "/",
						VerticalAlignment = VerticalAlignment.Center
					});
				}
			}

			row.Children.Add(iconContainer);

			row.Children.Add(new TextBlock
			{
				Text = description,
				VerticalAlignment = VerticalAlignment.Center
			});
			return row;
		}

		var content = new StackPanel { Spacing = 14 };
		content.Children.Add(CreateRow(["\uF108", "\uF10E",], "Navigate"));
		content.Children.Add(CreateRow(["\uF093"], "Select"));
		content.Children.Add(CreateRow(["\uF094"], "Go back"));
		content.Children.Add(CreateRow(["\uF10C"], "Jump to first game"));
		content.Children.Add(CreateRow(["\uF10D"], "Jump to last game"));

		var dialog = new ContentDialog
		{
			Title = "Gamepad Controls",
			Content = content,
			CloseButtonText = "Got it",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};

		await dialog.ShowAsync();

		if (activeInstance.Items.Count > 0)
		{
			(activeInstance.ContainerFromIndex(0) as Control)?.Focus(FocusState.Programmatic);
		}
	}

	protected override void OnItemsChanged(object e)
	{
		base.OnItemsChanged(e);
		SubscribeToEvents();
	}

	private void SubscribeToEvents()
	{
		foreach (HeaderCarouselItem tile in Items.Cast<HeaderCarouselItem>())
		{
			tile.PointerEntered -= Tile_PointerEntered;
			tile.PointerEntered += Tile_PointerEntered;

			tile.GotFocus -= Tile_GotFocus;
			tile.GotFocus += Tile_GotFocus;
		}
	}

	private void UnsubscribeToEvents()
	{
		selectionTimer.Tick -= SelectionTimer_Tick;
		selectionTimer?.Stop();
		gameWatcherTimer?.Stop();

		foreach (HeaderCarouselItem tile in Items.Cast<HeaderCarouselItem>())
		{
			tile.PointerEntered -= Tile_PointerEntered;
			tile.GotFocus -= Tile_GotFocus;
		}
	}

	private void SelectionTimer_Tick(object? sender, object e)
	{
		SelectNextTile();
	}

	private async void SelectNextTile()
	{
		if (Items.Count == 0)
		{
			return;
		}

		if (Items[GetNextUniqueRandom()] is HeaderCarouselItem tile)
		{
			selectedTile = tile;

			Panel panel = ItemsPanelRoot;
			if (panel != null && scrollViewer != null)
			{
				GeneralTransform transform = selectedTile.TransformToVisual(panel);
				Point point = transform.TransformPoint(new Point(0, 0));
				scrollViewer.ChangeView(point.X - (scrollViewer.ActualWidth / 2) + (selectedTile.ActualSize.X / 2), null, null);
			}

			SelectTile();
			await Task.Delay(500);
		}
	}

	private void ResetAndShuffle()
	{
		if (Items.Count == 0)
		{
			return;
		}

		numbers.Clear();
		for (int i = 0; i <= Items.Count - 1; i++)
		{
			numbers.Add(i);
		}

		// Shuffle the list
		for (int i = numbers.Count - 1; i > 0; i--)
		{
			int j = random.Next(i + 1);
			(numbers[j], numbers[i]) = (numbers[i], numbers[j]);
		}

		currentIndex = 0;
	}

	private int GetNextUniqueRandom()
	{
		if (currentIndex >= numbers.Count)
		{
			ResetAndShuffle();
		}

		int nextIndex = numbers[currentIndex++];

		if (selectedTile != null)
		{
			int selectedIndex = Items.IndexOf(selectedTile);
			if (nextIndex == selectedIndex)
			{
				if (currentIndex >= numbers.Count)
					ResetAndShuffle();
				nextIndex = numbers[currentIndex++];
			}
		}

		return nextIndex;
	}

	private void SetTileVisuals(bool playSound = false)
	{
		if (selectedTile != null)
		{
			selectedTile.IsSelected = true;
			if (playSound)
				ElementSoundPlayer.Play(ElementSoundKind.Focus);

			if (selectedTile.BackgroundImageUrl != null && backDropImage.ImageUrl?.ToString() != selectedTile.BackgroundImageUrl)
				backDropImage.ImageUrl = new Uri(selectedTile.BackgroundImageUrl);

			if (MetadataGrid.Visibility == Visibility.Collapsed)
				MetadataGrid.Visibility = Visibility.Visible;

			Metadata_ScrollViewer.ChangeView(null, 0, null);

			Play?.XYFocusUp = selectedTile!;
			Update?.XYFocusUp = selectedTile!;
			StopProcesses?.XYFocusUp = selectedTile!;
			RestartProcesses?.XYFocusUp = selectedTile!;

			if (selectedTile != null && SearchBox != null)
				selectedTile.XYFocusUp = SearchBox;

			SearchBox?.XYFocusDown = selectedTile!;
			Sort?.XYFocusDown = selectedTile!;
			EpicGamesButton?.XYFocusDown = selectedTile!;
			SteamButton?.XYFocusDown = selectedTile!;

			Title = selectedTile?.Title ?? "";
			Developers = selectedTile?.Developers ?? "";

			UpdateIsAvailable = selectedTile?.UpdateIsAvailable ?? false;

			Rating = selectedTile?.Rating is double r && r != 0.0 ? r : Rating;
			RoundedRating = Math.Round((selectedTile?.Rating ?? 0.0), 1).ToString("0.0", CultureInfo.InvariantCulture);
			PlayTime = selectedTile?.PlayTime ?? "";
			AgeRatingUrl = selectedTile?.AgeRatingUrl ?? "";
			AgeRatingTitle = selectedTile?.AgeRatingTitle ?? "";
			AgeRatingDescription = selectedTile?.AgeRatingDescription ?? "";
			AgeRatingDescriptionText.Visibility = string.IsNullOrEmpty(AgeRatingDescription)
				? Visibility.Collapsed
				: Visibility.Visible;

			Elements = selectedTile?.Elements ?? "";
			ElementsText.Visibility = string.IsNullOrEmpty(Elements)
				? Visibility.Collapsed
				: Visibility.Visible;

			Genres = selectedTile?.Genres ?? [];
			Features = selectedTile?.Features ?? [];
			Description = selectedTile?.Description ?? "";

			InstallLocation = selectedTile?.InstallLocation ?? "";

			Launcher = selectedTile?.Launcher ?? "";

			CatalogItemId = selectedTile?.CatalogItemId ?? "";
			CatalogNamespace = selectedTile?.CatalogNamespace ?? "";
			AppName = selectedTile?.AppName ?? "";
			LaunchExecutable = selectedTile?.LaunchExecutable ?? "";
			LaunchCommand = selectedTile?.LaunchCommand ?? "";
			ProcessNames = selectedTile?.ProcessNames!;
			ArtifactId = selectedTile?.ArtifactId ?? "";

			GameID = selectedTile?.GameID ?? "";

			LauncherLocation = selectedTile?.LauncherLocation ?? "";
			DataLocation = selectedTile?.DataLocation ?? "";
			GameLocation = selectedTile?.GameLocation ?? "";

			ReleaseDate = selectedTile?.ReleaseDate ?? "";
			Size = selectedTile?.Size ?? "";
			Version = selectedTile?.Version ?? "";

			InfoItems.Clear();

			InfoItems.Add(new InfoItem
			{
				Label = "Published by",
				Value = Developers,
				PathData = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), Application.Current.Resources["PackageIconPath"]),
				IconType = InfoIconType.PathIcon
			});

			InfoItems.Add(new InfoItem
			{
				Label = "Release date",
				Value = ReleaseDate,
				Glyph = "\uE787",
				IconType = InfoIconType.FontIcon
			});

			InfoItems.Add(new InfoItem
			{
				Label = "Approximate size",
				Value = Size,
				Glyph = "\uECAA",
				IconType = InfoIconType.FontIcon
			});

			if (!string.IsNullOrEmpty(Version))
			{
				InfoItems.Add(new InfoItem
				{
					Label = "Installed version",
					Value = Version,
					PathData = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), Application.Current.Resources["VersionIconPath"]),
					IconType = InfoIconType.PathIcon
				});
			}

			InfoItems.Add(new InfoItem
			{
				Label = "Install location",
				IsHyperlink = true,
				Hyperlink = InstallLocation,
				Glyph = "\uE8B7",
				IconType = InfoIconType.FontIcon
			});

			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
			{
				CheckGameRunning();
				Screenshots = selectedTile?.Screenshots!;
				Screenshots_Card.Visibility = (Screenshots?.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
				//Screenshots_Gallery.ResetScrollPosition();

				//Videos = selectedTile?.Videos;
				//Videos_ScrollViewer.Visibility = (Videos?.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
			});
		}
	}

	private void Tile_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		var tile = (HeaderCarouselItem)sender;
		if (!tile.IsLoaded)
			return;

		if (tile != selectedTile)
		{
			selectedTile = tile;
			SelectTile(true);
			tile.Focus(FocusState.Programmatic);
		}
		else
		{
			selectionTimer?.Stop();
		}
	}

	private void SelectTile(bool playSound = false)
	{
		selectionTimer.Stop();

		foreach (HeaderCarouselItem t in Items.Cast<HeaderCarouselItem>())
		{
			t.IsSelected = false;
		}

		if (selectedTile != null)
		{
			selectedTile.IsSelected = true;
		}

		SetTileVisuals(playSound);
	}

	private void Tile_GotFocus(object sender, RoutedEventArgs e)
	{
		var tile = (HeaderCarouselItem)sender;
		if (!tile.IsLoaded)
			return;

		if (selectedTile == tile && tile.IsSelected)
			return;

		selectedTile = tile;
		SelectTile(tile.FocusState != FocusState.Pointer);

		if (Items.IndexOf(tile) == Items.Count - 1)
		{
			scrollViewer?.ChangeView(scrollViewer.ScrollableWidth, null, null);
		}
		else if (Items.IndexOf(tile) == 0)
		{
			scrollViewer?.ChangeView(0, null, null);
		}
	}

	private void ApplyAutoScroll()
	{
		if (IsAutoScrollEnabled)
		{
			ResetAndShuffle();
			SelectNextTile();
		}
		else
		{
			selectionTimer?.Stop();
		}
	}

	private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
		{
			var suggestions = Items
				.OfType<HeaderCarouselItem>()
				.Where(g => !string.IsNullOrEmpty(g.Title) && g.Title.Contains(sender.Text, StringComparison.CurrentCultureIgnoreCase))
				.Select(g => g.Title)
				.Distinct()
				.ToList();

			sender.ItemsSource = suggestions;
		}
	}

	private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		string? query = args.QueryText?.Trim();
		if (string.IsNullOrEmpty(query))
			return;

		SelectTileByTitle(query);
	}

	private async void SelectTileByTitle(string title)
	{
		if (string.IsNullOrEmpty(title) || Items.Count == 0)
			return;

		HeaderCarouselItem? tile = Items
			.OfType<HeaderCarouselItem>()
			.FirstOrDefault(t => string.Equals(t.Title, title, StringComparison.CurrentCultureIgnoreCase));

		if (tile == null)
			return;

		if (selectedTile != null && selectedTile != tile)
		{
			selectedTile.IsSelected = false;
			selectedTile = null;
		}

		selectedTile = tile;

		UnsubscribeToEvents();

		Panel panel = ItemsPanelRoot;
		if (panel != null)
		{
			GeneralTransform transform = selectedTile.TransformToVisual(panel);
			Point point = transform.TransformPoint(new Point(0, 0));
			scrollViewer.ChangeView(point.X - (scrollViewer.ActualWidth / 2) + (selectedTile.ActualSize.X / 2), null, null);
			SetTileVisuals();
		}

		await Task.Delay(500);

		SubscribeToEvents();
	}

	private void SortKey_Click(object sender, RoutedEventArgs e)
	{
		if (sender is RadioMenuFlyoutItem item)
		{
			currentSortKey = item.Name switch
			{
				"SortByName" => "Title",
				"SortByLauncher" => "Launcher",
				"SortByRating" => "Rating",
				"SortByTimePlayed" => "Time Played",
				"SortByRecentlyPlayed" => "Recently Played",
				_ => currentSortKey
			};

			ApplySort();
			SaveSortSettings();
		}
	}

	private void SortOrder_Click(object sender, RoutedEventArgs e)
	{
		if (sender is RadioMenuFlyoutItem item)
		{
			ascending = item.Name == "SortAscending";
			ApplySort();
			SaveSortSettings();
		}
	}

	private async void ApplySort()
	{
		var items = Items.OfType<HeaderCarouselItem>().ToList();
		if (items.Count == 0)
			return;

		IEnumerable<HeaderCarouselItem> result = currentSortKey switch
		{
			"Title" => ascending
				? items.OrderBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
				: items.OrderByDescending(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
			"Launcher" => ascending
				? items.OrderBy(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
				: items.OrderByDescending(g => g.Launcher ?? "", StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
			"Rating" => ascending
				? items.OrderBy(g => g.Rating)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
				: items.OrderByDescending(g => g.Rating)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
			"Time Played" => ascending
				? items.OrderBy(g => ParseMinutes(g.PlayTime ?? ""))
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
				: items.OrderByDescending(g => ParseMinutes(g.PlayTime ?? ""))
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
			"Recently Played" => ascending
				? items.OrderBy(g =>
					localSettings.Values.TryGetValue($"LastPlayed_{g.Title}", out object? val) && val is long ts ? ts : 0)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase)
				: items.OrderByDescending(g =>
					localSettings.Values.TryGetValue($"LastPlayed_{g.Title}", out object? val) && val is long ts ? ts : 0)
				.ThenBy(g => g.Title ?? "", StringComparer.CurrentCultureIgnoreCase),
			_ => items
		};

		Items.Clear();
		foreach (HeaderCarouselItem item in result)
		{
			Items.Add(item);
		}

		await Task.Delay(100);
		SelectTileByTitle(Title);
	}

	private void SaveSortSettings()
	{
		ApplicationData.Current.LocalSettings.Values["SortKey"] = currentSortKey;
		ApplicationData.Current.LocalSettings.Values["SortAscending"] = ascending;
	}

	private void LoadSortSettings()
	{
		IPropertySet settings = ApplicationData.Current.LocalSettings.Values;
		currentSortKey = settings["SortKey"] as string ?? "Time Played";

		ascending = settings["SortAscending"] as bool? ?? false;

		SortByName.IsChecked = currentSortKey == "Title";
		SortByLauncher.IsChecked = currentSortKey == "Launcher";
		SortByRating.IsChecked = currentSortKey == "Rating";
		SortByTimePlayed.IsChecked = currentSortKey == "Time Played";
		SortByRecentlyPlayed.IsChecked = currentSortKey == "Recently Played";

		SortAscending.IsChecked = ascending;
		SortDescending.IsChecked = !ascending;

		ApplySort();
	}

	[GeneratedRegex(@"(?:(\d+)h)?\s*(\d+)m", RegexOptions.Compiled)]
	private static partial Regex PlayTimeMinutesRegex();
	private static int ParseMinutes(string time)
	{
		if (string.IsNullOrWhiteSpace(time))
			return 0;

		Match match = PlayTimeMinutesRegex().Match(time);
		if (match.Success)
		{
			int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
			int minutes = int.Parse(match.Groups[2].Value);
			return hours * 60 + minutes;
		}

		return 0;
	}

	public void LoadEpicGamesAccounts()
	{
		if (File.Exists(EpicGamesHelper.EpicGamesPath))
		{
			// get all accounts
			List<EpicGamesHelper.EpicAccountInfo> accounts = EpicGamesHelper.GetEpicGamesAccounts();

			// reset ui elements
			EpicGamesAccounts.Items.Clear();
			EpicGamesAccounts.IsEnabled = accounts.Count > 0;
			RemoveEpicGamesAccount.IsEnabled = accounts.Count > 0;

			// add accounts to combobox
			if (accounts.Count == 0)
			{
				var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
				EpicGamesAccounts.Items.Add(notLoggedIn);
				EpicGamesAccounts.SelectedItem = notLoggedIn;
				EpicGamesAccounts.IsEnabled = false;
				RemoveEpicGamesAccount.IsEnabled = false;
			}
			else if (!accounts.Any(a => a.IsActive))
			{
				var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
				EpicGamesAccounts.Items.Add(notLoggedIn);
				EpicGamesAccounts.SelectedItem = notLoggedIn;
				RemoveEpicGamesAccount.IsEnabled = false;

				foreach (EpicGamesHelper.EpicAccountInfo account in accounts)
				{
					var item = new ComboBoxItem
					{
						Content = account.DisplayName,
						Tag = account.AccountId
					};

					EpicGamesAccounts.Items.Add(item);
				}
			}
			else
			{
				foreach (EpicGamesHelper.EpicAccountInfo account in accounts)
				{
					var item = new ComboBoxItem
					{
						Content = account.DisplayName,
						Tag = account.AccountId
					};

					EpicGamesAccounts.Items.Add(item);

					if (account.IsActive)
						EpicGamesAccounts.SelectedItem = item;
				}
			}
		}
		else
		{
			EpicGamesButton.Visibility = Visibility.Collapsed;
		}

		isInitializingEpicGamesAccounts = false;
	}

	private async void EpicGamesAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingEpicGamesAccounts)
			return;

		// close epic games launcher
		EpicGamesHelper.CloseEpicGames();

		// update config before switching
		if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
		{
			(string? oldAccountId, string _, string _, int _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

			string accountDir = Path.Combine(EpicGamesHelper.EpicGamesAccountDir, oldAccountId ?? "");
			if (Directory.Exists(accountDir))
				File.Copy(EpicGamesHelper.ActiveEpicGamesAccountPath, Path.Combine(accountDir, "GameUserSettings.ini"), true);
		}

		// get accountId
		string? accountId = (EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Tag as string;

		// replace file
		File.Copy(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId ?? "", "GameUserSettings.ini"), EpicGamesHelper.ActiveEpicGamesAccountPath, true);

		// replace accountid
		Process.Start("regedit.exe", $@"/s ""{Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId ?? "", "accountId.reg")}""");

		// update refresh token
		if (await EpicGamesHelper.UpdateEpicGamesToken(EpicGamesHelper.ActiveEpicGamesAccountPath) == null)
		{
			UpdateInvalidEpicGamesToken();
			return;
		}

		// close epic games launcher
		EpicGamesHelper.CloseEpicGames();

		// refresh combobox
		isInitializingEpicGamesAccounts = true;
		LoadEpicGamesAccounts();

		// refresh library
		foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games" || item.Launcher == "Ubisoft Connect" || item.Launcher == "The EA App" || item.Launcher == "Origin").ToList())
			Items.Remove(item);

		AddGames(await EpicGamesHelper.GetGames());

		LoadSortSettings();
		NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		SwitchPresenter.Value = false;
	}

	private async void UpdateInvalidEpicGamesToken()
	{
		// add growl
		Growl.Info(new GrowlInfo
		{
			ShowDateTime = false,
			StaysOpen = true,
			IsClosable = false,
			UseBlueColorForInfo = true,
			Title = "The refresh token is no longer valid. Please enter your password again...",
			Token = "Epic"
		});

		// close epic games launcher
		EpicGamesHelper.CloseEpicGames();

		// delay
		await Task.Delay(500);

		// launch epic games launcher
		Process.Start(EpicGamesHelper.EpicGamesPath);

		// delay
		await Task.Delay(2000);

		// check when logged in
		while (true)
		{
			if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
			{
				if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
				{
					break;
				}
			}

			await Task.Delay(500);
		}

		// close epic games launcher
		EpicGamesHelper.CloseEpicGames();

		// disable tray and notifications
		EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
		EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

		// clear
		Growl.Clear("Epic");

		// add growl
		Growl.Success(new GrowlInfo
		{
			ShowDateTime = false,
			StaysOpen = false,
			IsClosable = false,
			Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}.",
			Token = "Epic"
		});

		// refresh combobox
		isInitializingEpicGamesAccounts = true;
		LoadEpicGamesAccounts();

		// refresh library
		foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games" || item.Launcher == "Ubisoft Connect" || item.Launcher == "The EA App" || item.Launcher == "Origin").ToList())
			Items.Remove(item);

		AddGames(await EpicGamesHelper.GetGames());

		LoadSortSettings();
		NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		SwitchPresenter.Value = false;
	}

	private async void AddEpicGamesAccount_Click(object sender, RoutedEventArgs e)
	{
		// add content dialog
		var contentDialog = new ContentDialog
		{
			Title = "Add Epic Games Account",
			Content = "Are you sure that you want to add an Epic Games account?",
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = this.XamlRoot
		};

		ContentDialogResult result = await contentDialog.ShowAsync();

		// check result
		if (result == ContentDialogResult.Primary)
		{
			// add growl
			Growl.Info(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = true,
				IsClosable = false,
				UseBlueColorForInfo = true,
				Title = "Please log in to your Epic Games account...",
				Token = "Epic"
			});

			// close epic games launcher
			EpicGamesHelper.CloseEpicGames();

			// delete gameusersettings.ini
			if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
			{
				File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
			}

			// delay
			await Task.Delay(500);

			// launch epic games launcher
			Process.Start(EpicGamesHelper.EpicGamesPath);

			// check when logged in
			while (true)
			{
				if (File.Exists(EpicGamesHelper.ActiveEpicGamesAccountPath))
				{
					if (EpicGamesHelper.ValidateData(EpicGamesHelper.ActiveEpicGamesAccountPath))
					{
						break;
					}
				}

				await Task.Delay(500);
			}

			// close epic games launcher
			EpicGamesHelper.CloseEpicGames();

			// disable tray and notifications
			EpicGamesHelper.DisableMinimizeToTray(EpicGamesHelper.ActiveEpicGamesAccountPath);
			EpicGamesHelper.DisableNotifications(EpicGamesHelper.ActiveEpicGamesAccountPath);

			// clear
			Growl.Clear("Epic");

			// add growl
			Growl.Success(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = false,
				Title = $"Successfully logged in as {EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).DisplayName}.",
				Token = "Epic"
			});

			// refresh combobox
			isInitializingEpicGamesAccounts = true;
			LoadEpicGamesAccounts();

			// refresh library
			foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games" || item.Launcher == "Ubisoft Connect" || item.Launcher == "The EA App" || item.Launcher == "Origin").ToList())
				Items.Remove(item);

			AddGames(await EpicGamesHelper.GetGames());

			LoadSortSettings();
			NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			SwitchPresenter.Value = false;
		}
	}

	private async void RemoveEpicGamesAccount_Click(object sender, RoutedEventArgs e)
	{
		// add content dialog
		var contentDialog = new ContentDialog
		{
			Title = "Remove Epic Games Account",
			Content = $"Are you sure that you want to remove {(EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Content}?",
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = this.XamlRoot,
		};
		ContentDialogResult result = await contentDialog.ShowAsync();

		// check results
		if (result == ContentDialogResult.Primary)
		{
			// close epic games launcher
			EpicGamesHelper.CloseEpicGames();

			// get accountId
			string accountId = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath).AccountId ?? "";

			// remove account
			File.Delete(EpicGamesHelper.ActiveEpicGamesAccountPath);
			Directory.Delete(Path.Combine(EpicGamesHelper.EpicGamesAccountDir, accountId), true);

			// add growl
			Growl.Success(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = false,
				Title = $"Successfully removed {(EpicGamesAccounts.SelectedItem as ComboBoxItem)?.Content}.",
				Token = "Epic",
			});

			// refresh combobox
			isInitializingEpicGamesAccounts = true;
			LoadEpicGamesAccounts();

			// remove all epic games titles
			foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Epic Games").ToList())
				Items.Remove(item);
		}
	}

	public void LoadSteamAccounts()
	{
		if (File.Exists(SteamHelper.SteamPath))
		{
			// get all accounts
			List<SteamHelper.SteamAccountInfo> accounts = SteamHelper.GetSteamAccounts();

			// reset ui elements
			SteamAccounts.Items.Clear();
			SteamAccounts.IsEnabled = true;
			RemoveSteamAccount.IsEnabled = true;

			// add accounts to combobox
			if (accounts.Count == 0)
			{
				var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
				SteamAccounts.Items.Add(notLoggedIn);
				SteamAccounts.SelectedItem = notLoggedIn;
				SteamAccounts.IsEnabled = false;
				RemoveSteamAccount.IsEnabled = false;
			}
			else if (accounts.All(a => !a.MostRecent) || accounts.All(a => !a.AllowAutoLogin))
			{
				var notLoggedIn = new ComboBoxItem { Content = "Not logged in", IsEnabled = false };
				SteamAccounts.Items.Add(notLoggedIn);
				SteamAccounts.SelectedItem = notLoggedIn;
				RemoveSteamAccount.IsEnabled = false;

				foreach (SteamHelper.SteamAccountInfo account in accounts)
				{
					SteamAccounts.Items.Add(account.AccountName);
				}
			}
			else
			{
				foreach (SteamHelper.SteamAccountInfo account in accounts)
				{
					SteamAccounts.Items.Add(account.AccountName);
				}

				int selectedIndex = accounts.FindIndex(a => a.MostRecent);
				if (selectedIndex < 0)
					selectedIndex = accounts.FindIndex(a => a.AllowAutoLogin);

				SteamAccounts.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
			}
		}
		else
		{
			SteamButton.Visibility = Visibility.Collapsed;
		}

		isInitializingSteamAccounts = false;
	}

	private async void SteamAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (isInitializingSteamAccounts)
			return;

		// close steam
		SteamHelper.CloseSteam();

		// read file
		string content = File.ReadAllText(SteamHelper.SteamLoginUsersPath);
		KVDocument kv;
		try
		{
			kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = true });
		}
		catch (KeyValueException)
		{
			kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = false });
		}

		// make all accounts inactive
		foreach (KeyValuePair<string, KVObject> user in kv.Root.Children)
		{
			if (user.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem?.ToString())
			{
				user.Value["MostRecent"] = "1";
				user.Value["AllowAutoLogin"] = "1";
				user.Value["Timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
			}
			else
			{
				user.Value["MostRecent"] = "0";
				user.Value["AllowAutoLogin"] = "0";
			}
		}

		// write changes
		using var msOut = new MemoryStream();
		KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
		msOut.Position = 0;
		File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

		// update registry key
		Registry.SetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "AutoLoginUser", SteamAccounts.SelectedItem?.ToString() ?? "", RegistryValueKind.String);

		// refresh combobox
		isInitializingSteamAccounts = true;
		LoadSteamAccounts();

		// refresh library
		foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Steam").ToList())
			Items.Remove(item);

		AddGames(await SteamHelper.GetGames());

		LoadSortSettings();
		NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		SwitchPresenter.Value = false;
	}

	private async void AddSteamAccount_Click(object sender, RoutedEventArgs e)
	{
		// add content dialog
		var contentDialog = new ContentDialog
		{
			Title = "Add Steam Account",
			Content = "Are you sure that you want to add a Steam account?",
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = this.XamlRoot
		};

		ContentDialogResult result = await contentDialog.ShowAsync();

		// check result
		if (result == ContentDialogResult.Primary)
		{
			// add growl
			Growl.Info(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = true,
				IsClosable = false,
				UseBlueColorForInfo = true,
				Title = "Info",
				Token = "Steam",
				Message = "Please log in to your Steam account..."
			});

			// close steam
			SteamHelper.CloseSteam();

			// read file
			if (File.Exists(SteamHelper.SteamLoginUsersPath))
			{
				string content = File.ReadAllText(SteamHelper.SteamLoginUsersPath);
				KVDocument kv;
				try
				{
					kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = true });
				}
				catch (KeyValueException)
				{
					kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = false });
				}

				// make all accounts inactive
				foreach (KeyValuePair<string, KVObject> user in kv.Root.Children)
				{
					user.Value["MostRecent"] = "0";
					user.Value["AllowAutoLogin"] = "0";
				}

				// write changes
				using var msOut = new MemoryStream();
				KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, kv);
				msOut.Position = 0;
				File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());
			}

			// delay
			await Task.Delay(500);

			// get initial user count
			int initialUserCount = 0;
			if (File.Exists(SteamHelper.SteamLoginUsersPath))
			{
				string initialContent = File.ReadAllText(SteamHelper.SteamLoginUsersPath);
				KVDocument initialKv;
				try
				{
					initialKv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(initialContent)), new KVSerializerOptions { HasEscapeSequences = true });
				}
				catch (KeyValueException)
				{
					initialKv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(initialContent)), new KVSerializerOptions { HasEscapeSequences = false });
				}
				initialUserCount = initialKv.Root.Children.Count();
			}

			// launch steam
			Process.Start(SteamHelper.SteamPath);

			// check when logged in
			while (true)
			{
				if (File.Exists(SteamHelper.SteamLoginUsersPath))
				{
					string loginContent = File.ReadAllText(SteamHelper.SteamLoginUsersPath);
					KVDocument loginKv;
					try
					{
						loginKv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(loginContent)), new KVSerializerOptions { HasEscapeSequences = true });
					}
					catch (KeyValueException)
					{
						loginKv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(loginContent)), new KVSerializerOptions { HasEscapeSequences = false });
					}
					if (loginKv.Root.Children.Count() > initialUserCount)
						break;
				}

				await Task.Delay(500);
			}

			// close steam
			SteamHelper.CloseSteam();

			// refresh combobox
			isInitializingSteamAccounts = true;
			LoadSteamAccounts();

			// refresh library
			foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Steam").ToList())
				Items.Remove(item);

			AddGames(await SteamHelper.GetGames());

			LoadSortSettings();
			NoGames_StackPanel.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
			SwitchPresenter.Value = false;

			// clear
			Growl.Clear("Steam");

			// add growl
			Growl.Success(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = false,
				Title = $"Successfully logged in as {SteamAccounts.SelectedItem}",
				Token = "Steam"
			});
		}
	}

	private async void RemoveSteamAccount_Click(object sender, RoutedEventArgs e)
	{
		// add content dialog
		var contentDialog = new ContentDialog
		{
			Title = "Remove Steam Account",
			Content = $"Are you sure that you want to remove {SteamAccounts.SelectedItem}?",
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot,
		};
		ContentDialogResult result = await contentDialog.ShowAsync();

		// check results
		if (result == ContentDialogResult.Primary)
		{
			// close steam
			SteamHelper.CloseSteam();

			// read file
			string content = File.ReadAllText(SteamHelper.SteamLoginUsersPath);
			KVDocument kv;
			try
			{
				kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = true });
			}
			catch (KeyValueException)
			{
				kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(new MemoryStream(Encoding.UTF8.GetBytes(content)), new KVSerializerOptions { HasEscapeSequences = false });
			}

			// remove selected account
			KeyValuePair<string, KVObject> selectedChild = kv.Root.Children.FirstOrDefault(child => child.Value["AccountName"]?.ToString() == SteamAccounts.SelectedItem?.ToString());

			if (selectedChild.Key is null)
				return;

			var newChildren = kv.Root.Children.Where(children => children.Key != selectedChild.Key).ToList();

			var newRoot = new KVObject();

			foreach (var child in newChildren)
			{
				newRoot[child.Key] = child.Value;
			}

			// write changes
			using var msOut = new MemoryStream();
			KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(msOut, new KVDocument(null, kv.Name, newRoot));
			msOut.Position = 0;
			File.WriteAllText(SteamHelper.SteamLoginUsersPath, new StreamReader(msOut).ReadToEnd());

			// add growl
			Growl.Success(new GrowlInfo
			{
				ShowDateTime = false,
				StaysOpen = false,
				IsClosable = false,
				Title = $"Successfully removed {SteamAccounts.SelectedItem}.",
				Token = "Epic",
			});

			// refresh combobox
			isInitializingSteamAccounts = true;
			LoadSteamAccounts();

			// remove all steam titles
			foreach (HeaderCarouselItem? item in Items.OfType<HeaderCarouselItem>().Where(item => item.Launcher == "Steam").ToList())
				Items.Remove(item);
		}
	}


	private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
	{
		if (args.Element is FrameworkElement container)
		{
			var button = container.FindName("Link") as HyperlinkButton;
			if (button != null)
			{
				button.Click -= Link_Click;
				button.Click += Link_Click;
			}
		}
	}

	private async void Link_Click(object sender, RoutedEventArgs e)
	{
		if (sender is HyperlinkButton button && button.DataContext is InfoItem item)
		{
			if (!string.IsNullOrEmpty(item.Hyperlink))
			{
				await Windows.System.Launcher.LaunchFolderPathAsync(item.Hyperlink.Trim());
			}
		}
	}

	private async void Play_Click(object sender, RoutedEventArgs e)
	{
		selectionTimer?.Stop();

		HeaderCarouselItem? tile = selectedTile;
		if (tile == null)
			return;

		string launcher = tile.Launcher ?? "";
		string installLocation = tile.InstallLocation ?? "";
		string launchExecutable = tile.LaunchExecutable ?? "";
		string launchCommand = tile.LaunchCommand ?? "";
		string launcherLocation = tile.LauncherLocation ?? "";
		string gameLocation = tile.GameLocation ?? "";
		string dataLocation = tile.DataLocation ?? "";
		string gameId = tile.GameID ?? "";
		string appName = tile.AppName ?? "";
		string catalogNamespace = tile.CatalogNamespace ?? "";
		string catalogItemId = tile.CatalogItemId ?? "";
		string artifactId = tile.ArtifactId ?? "";

		if (launcher == "Epic Games")
		{
			string exchangeCode = await EpicGamesHelper.Exchange();
			(string? accountId, string? displayName, string _, int _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

			string arguments = $@"{launchCommand} -AUTH_LOGIN=unused -AUTH_PASSWORD={exchangeCode} -AUTH_TYPE=exchangeCode -epicenv=Prod -EpicPortal -epicapp={appName} -epicusername={displayName} -epicuserid={accountId} -epiclocale=en -epicsandboxid={catalogNamespace}";

			var iniHelper = new InIHelper(EpicGamesHelper.ActiveEpicGamesAccountPath);
			string settingsSection = $"{accountId}_Settings";

			if (iniHelper.ReadValue($"{catalogNamespace}:{catalogItemId}:{appName}_AdditionalCommandsEnabled", settingsSection) == "True")
			{
				string additionalCommands = iniHelper.ReadValue($"{catalogNamespace}:{catalogItemId}:{appName}_AdditionalCommands", settingsSection);

				if (!string.IsNullOrEmpty(additionalCommands))
				{
					arguments += $" {additionalCommands}";
				}
			}

			var startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(installLocation, launchExecutable),
				Arguments = arguments,
				WorkingDirectory = Path.GetDirectoryName(Path.Combine(installLocation, launchExecutable)),
				UseShellExecute = false
			};

			Process.Start(startInfo);
		}
		else if (launcher == "Ubisoft Connect")
		{
			Process.Start(new ProcessStartInfo($"uplay://launch/{gameId}/0") { UseShellExecute = true });
		}
		else if (launcher == "The EA App" || launcher == "Origin")
		{
			string exchangeCode = await EpicGamesHelper.Exchange();
			(string? accountId, string? displayName, string _, int _) = EpicGamesHelper.GetAccountData(EpicGamesHelper.ActiveEpicGamesAccountPath);

			var startInfo = new ProcessStartInfo
			{
				FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Electronic Arts", "EA Desktop", "EA Desktop", "Link2EA.exe"),
				Arguments = $@"""link2ea://launchgame/{appName}?AUTH_PASSWORD={exchangeCode}&AUTH_TYPE=exchangeCode&epicusername={Uri.EscapeDataString(displayName ?? "")}&epicuserid={accountId}&epiclocale=en&platform=epic"" """" """" """" """" """" """" """" """"",
				WorkingDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Electronic Arts", "EA Desktop", "EA Desktop"),
				UseShellExecute = false
			};
			Process.Start(startInfo);
		}
		else if (launcher == "Steam")
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = SteamHelper.SteamPath,
				Arguments = $"-silent steam://rungameid/{gameId} "
			});
		}
		else if (launcher == "Eden")
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = launcherLocation,
				Arguments = $@"-f -g ""{gameLocation}""",
				CreateNoWindow = true,
			};

			Process.Start(startInfo);
		}
		else if (launcher == "Citron")
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = launcherLocation,
				Arguments = $@"-f -g ""{gameLocation}""",
				CreateNoWindow = true,
			};

			Process.Start(startInfo);
		}
		else if (launcher == "Ryujinx")
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = launcherLocation,
				Arguments = $@"-r ""{dataLocation}"" -fullscreen ""{gameLocation}""",
				CreateNoWindow = true,
			};

			Process.Start(startInfo);
		}

		localSettings.Values[$"LastPlayed_{tile.Title}"] = DateTimeOffset.Now.ToUnixTimeSeconds();

		if (currentSortKey.Equals("Recently Played", StringComparison.OrdinalIgnoreCase))
		{
			LoadSortSettings();
		}
		else
		{
			if (selectedTile != null)
			{
				int idx = Items.IndexOf(selectedTile);
				if (idx >= 0)
					(ContainerFromIndex(idx) as Control)?.Focus(FocusState.Programmatic);
			}
		}
	}

	private async void Update_Click(object sender, RoutedEventArgs e)
	{
		if (Launcher == "Epic Games")
		{
			Process.Start(new ProcessStartInfo($"com.epicgames.launcher://apps/{CatalogNamespace}%3A{CatalogItemId}%3A{AppName}?action=update") { UseShellExecute = true });
		}
	}

	private async void StopProcesses_Click(object sender, RoutedEventArgs e)
	{
		// disable hittestvisible to avoid double-clicking
		StopProcesses.IsHitTestVisible = false;
		RestartProcesses.IsHitTestVisible = false;

		await Task.Run(() =>
		{

			// close dllhost processes
			foreach (Process proc in Process.GetProcessesByName("dllhost"))
			{
				string cmdLine = ProcessesHelper.GetCommandLine(proc);

				if (cmdLine.Contains("/PROCESSID", StringComparison.OrdinalIgnoreCase))
				{
					proc.Kill();
					proc.WaitForExit();
				}
			}

			// close executables
			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 0, RegistryValueKind.DWord);

			string[] processNames = new[]
			{
				"ApplicationFrameHost",
				//"backgroundTaskHost",
				"CrashReportClient",
				"CrossDeviceResume",
				//"ctfmon",
				"DataExchangeHost",
				"EasyAntiCheat_EOS",
				"EpicGamesLauncher",
				"EpicOnlineServicesUserHelper",
				"explorer",
				"Everything",
				//"Files",
				"FortniteBootstrapper",
				"FortniteClient-Win64-Shipping_EAC_EOS",
				"GameBar",
				"GameBarFTServer",
				"gamingservices",
				"gamingservicesnet",
				"LeagueCrashHandler64",
				"LsaIso",
				"mobsync",
				"NgcIso",
				"RiotClientServices",
				"RiotClientCrashHandler",
				"rundll32",
				"RuntimeBroker",
				"SearchHost",
				"secd",
				"ShellExperienceHost",
				"SpatialAudioLicenseSrv",
				"sppsvc",
				"StartMenuExperienceHost",
				"SystemSettingsBroker",
				"TabTip",
				"TextInputHost",
				"TrustedInstaller",
				"useroobebroker",
				//"WMIADAP",
				//"WmiPrvSE",
				"WUDFHost",
				"XboxPcAppFT"
			};

			foreach (string name in processNames)
			{
				foreach (Process process in Process.GetProcessesByName(name))
				{
					process.Kill();
					process.WaitForExit();
				}
			}

			Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", "AutoRestartShell", 1, RegistryValueKind.DWord);

			// stop services
			string[] serviceNames = new[]
			{
				"AudioEndpointBuilder",
				"Appinfo",
				"AppXSvc",
				"CaptureService",
				"cbdhsvc",
				//"CDPSvc",
				//"CDPUserSvc",
				"ClipSvc",
				"CryptSvc",
				"DevicesFlowUserSvc",
				"DeviceAssociationService",
				"DeviceInstall",
				"DispBrokerDesktopSvc",
				"DoSvc",
				"Everything (1.5a)",
				"gpsvc",
				"InstallService",
				"KeyIso",
				"LicenseManager",
				"lfsvc",
				"msiserver",
				//"NcbService",
				"Netman",
				"NetSetupSvc",
				"netprofm",
				"NgcCtnrSvc",
				"NgcSvc",
				"NVDisplay.ContainerLocalSystem",
				"ProfSvc",
				//"Schedule",
				//"SstpSvc",
				"StateRepository",
				//"TimeBrokerSvc",
				//"TokenBroker",
				"TrustedInstaller",
				"UdkUserSvc",
				"UserManager",
				"WFDSConMgrSvc",
				"Windhawk",
				//"Winmgmt",
				"WpnService",
				"WpnUserService"
			};

			foreach (string serviceName in serviceNames)
			{
				try
				{
					ServicesHelper.StopService(serviceName);
				}
				catch
				{
					ServicesHelper.KillServiceProcess(serviceName);
				}
			}

			foreach (string serviceName in serviceNames)
			{
				try
				{
					ServicesHelper.StopService(serviceName);
				}
				catch
				{
					ServicesHelper.KillServiceProcess(serviceName);
				}
			}

			if (Process.GetProcessesByName("ClassicWindowSwitcher").Length == 0)
				Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Applications", "ClassicWindowSwitcher", "ClassicWindowSwitcher.exe")) { CreateNoWindow = true });
		});

		// re-enable hittestvisible
		StopProcesses.IsHitTestVisible = true;
		RestartProcesses.IsHitTestVisible = true;
	}

	private async void RestartProcesses_Click(object sender, RoutedEventArgs e)
	{
		// disable hittestvisible to avoid double-clicking
		StopProcesses.IsHitTestVisible = false;
		RestartProcesses.IsHitTestVisible = false;

		try
		{
			Process.GetProcessesByName("ClassicWindowSwitcher").FirstOrDefault()?.Kill();

			// launch explorer
			Process.Start("explorer.exe");

			// start windhawk service
			ServicesHelper.StartService("Windhawk");
		}
		catch { }

		// restart services
		string[] serviceNames = new[]
		{
			"AudioEndpointBuilder",
			"Appinfo",
			"AppXSvc",
			"CaptureService",
			"cbdhsvc",
			//"CDPSvc",
			//"CDPUserSvc",
			"ClipSvc",
			"CryptSvc",
			"DevicesFlowUserSvc",
			"DeviceAssociationService",
			"DeviceInstall",
			"DispBrokerDesktopSvc",
			"DoSvc",
			"Everything (1.5a)",
			"gpsvc",
			"InstallService",
			"KeyIso",
			"LicenseManager",
			"lfsvc",
			"msiserver",
			//"NcbService",
			"Netman",
			"NetSetupSvc",
			"netprofm",
			"NgcCtnrSvc",
			"NgcSvc",
			"NVDisplay.ContainerLocalSystem",
			"ProfSvc",
			//"Schedule",
			//"SstpSvc",
			"StateRepository",
			//"TimeBrokerSvc",
			//"TokenBroker",
			"TrustedInstaller",
			"UdkUserSvc",
			"UserManager",
			"WFDSConMgrSvc",
			//"Winmgmt",
			"WpnService",
			"WpnUserService"
		};

		foreach (string serviceName in serviceNames)
		{
			try
			{
				ServicesHelper.StartService(serviceName);
			}
			catch { }
		}

		//ProcessesHelper.ResumeProcess(ServicesHelper.GetServicePid("TextInputManagementService"));
		//Process.Start("ctfmon.exe");

		string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Everything 1.5a", "Everything.exe");

		if (File.Exists(filePath))
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = filePath,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = "-startup",
			});
		}

		// re-enable hittestvisible
		StopProcesses.IsHitTestVisible = true;
		RestartProcesses.IsHitTestVisible = true;
	}

	private DispatcherTimer? gameWatcherTimer;
	private bool? previousGameState = null;
	private bool? previousExplorerState = null;
	private bool servicesState = false;
	private readonly Dictionary<string, DateTime> epicGameStartTimes = [];

	void StartGameWatcher(Func<bool> isGameRunning)
	{
		if (gameWatcherTimer != null)
		{
			gameWatcherTimer.Stop();
			gameWatcherTimer = null;
		}

		previousGameState = null;
		previousExplorerState = null;

		servicesState = new ServiceController("Beep").Status == ServiceControllerStatus.Running;

		gameWatcherTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500)
		};

		void TickHandler()
		{
			bool isRunning = isGameRunning();
			bool explorerRunning = Process.GetProcessesByName("explorer").Length > 0;

			DispatcherQueue.TryEnqueue(() =>
			{
				Play?.IsEnabled = !isRunning;

				if (StopProcesses != null && !servicesState)
					StopProcesses.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;

				if (RestartProcesses != null && !servicesState && previousExplorerState != (isRunning && !explorerRunning))
				{
					RestartProcesses.Visibility = (isRunning && !explorerRunning) ? Visibility.Visible : Visibility.Collapsed;
					previousExplorerState = isRunning && !explorerRunning;
				}

				if (previousGameState == true && isRunning == false && !explorerRunning)
				{
					RestartProcesses_Click(this, new RoutedEventArgs());
				}

				if (Launcher == "Epic Games")
				{
					if (isRunning && !epicGameStartTimes.ContainsKey(ArtifactId))
					{
						epicGameStartTimes[ArtifactId] = DateTime.Now;
					}
					else if (!isRunning && epicGameStartTimes.TryGetValue(ArtifactId, out DateTime startTime))
					{
						EpicGamesHelper.AddPlaytime(ArtifactId, startTime, (artId, newPlaytime) =>
						{
							foreach (object? item in Items)
							{
								if (item is HeaderCarouselItem tile && tile.ArtifactId == artId)
								{
									tile.PlayTime = newPlaytime;
									break;
								}
							}

							if (selectedTile != null && selectedTile.ArtifactId == artId)
							{
								PlayTime = newPlaytime;
							}
						});
						epicGameStartTimes.Remove(ArtifactId);
					}
				}

				previousGameState = isRunning;
			});
		}

		gameWatcherTimer.Tick += (s, e) => TickHandler();

		TickHandler();
		gameWatcherTimer.Start();
	}

	public void CheckGameRunning()
	{
		previousGameState = null;
		previousExplorerState = null;

		if (Launcher == "Epic Games")
		{
			string offlineExecutable = Path.GetFileNameWithoutExtension(LaunchExecutable);
			string onlineExecutable = Title switch
			{
				"Fortnite" => "FortniteClient-Win64-Shipping",
				"Fall Guys" => "FallGuys_client_game",
				_ => string.Empty
			};
			if (Title == "Fall Guys")
				offlineExecutable = "FallGuys_client";

			StartGameWatcher(() =>
			{
				Process[] running = Process.GetProcesses().Where(p => !p.ProcessName.Equals("EpicGamesLauncher", StringComparison.OrdinalIgnoreCase)).ToArray();
				return (!string.IsNullOrEmpty(offlineExecutable) && running.Any(p => p.ProcessName.Contains(Path.GetFileNameWithoutExtension(offlineExecutable), StringComparison.OrdinalIgnoreCase))) ||
			(!string.IsNullOrEmpty(onlineExecutable) && running.Any(p => p.ProcessName.Contains(Path.GetFileNameWithoutExtension(onlineExecutable), StringComparison.OrdinalIgnoreCase))) ||
			(ProcessNames?.Any(process => !string.IsNullOrEmpty(process) && running.Any(p => p.ProcessName.Contains(Path.GetFileNameWithoutExtension(process), StringComparison.OrdinalIgnoreCase))) ?? false);
			});
		}
		else if (Launcher == "Ubisoft Connect")
		{
			StartGameWatcher(() => Process.GetProcessesByName("UbisoftGameLauncher").Any(process => ProcessesHelper.GetCommandLine(process).Contains($"-upc_uplay_id {GameID}", StringComparison.OrdinalIgnoreCase)));
		}
		else if (Launcher == "The EA App" || Launcher == "Origin")
		{
			StartGameWatcher(() => Process.GetProcessesByName("EAEgsProxy").Any(process => ProcessesHelper.GetCommandLine(process).Contains(InstallLocation, StringComparison.OrdinalIgnoreCase)));
		}
		else if (Launcher == "Steam")
		{
			StartGameWatcher(() =>
			{
				Process[] running = Process.GetProcesses();
				if (ProcessNames != null && ProcessNames.Count != 0)
				{
					if (ProcessNames.Any(name => name != null && running.Any(p => p.ProcessName.Contains(Path.GetFileNameWithoutExtension(name), StringComparison.OrdinalIgnoreCase))))
						return true;
				}

				if (!string.IsNullOrEmpty(InstallLocation) && Directory.Exists(InstallLocation))
				{
					string[] exeFiles = Directory.GetFiles(InstallLocation, "*.exe", SearchOption.AllDirectories);
					var exeNames = exeFiles.Select(Path.GetFileNameWithoutExtension).Distinct().ToList();
					var exeSet = new HashSet<string>(exeFiles, StringComparer.OrdinalIgnoreCase);

					if (exeNames.Any(name => Process.GetProcessesByName(name).Any(process => exeSet.Contains(ProcessesHelper.GetProcessPath(process)))))
						return true;
				}

				return false;
			});
		}
		else if (Launcher == "Eden")
		{
			StartGameWatcher(() =>
			{
				foreach (Process proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
				{
					string cmdLine = ProcessesHelper.GetCommandLine(proc);

					if (cmdLine.Contains($@"-f -g ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			});
		}
		else if (Launcher == "Citron")
		{
			StartGameWatcher(() =>
			{
				foreach (Process proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
				{
					string cmdLine = ProcessesHelper.GetCommandLine(proc);

					if (cmdLine.Contains($@"-f -g ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			});
		}
		else if (Launcher == "Ryujinx")
		{
			StartGameWatcher(() =>
			{
				foreach (Process proc in Process.GetProcessesByName(Path.GetFileName(LauncherLocation).Replace(".exe", "")))
				{
					string cmdLine = ProcessesHelper.GetCommandLine(proc);

					if (cmdLine.Contains($@"-r ""{DataLocation}"" -fullscreen ""{GameLocation}""", StringComparison.OrdinalIgnoreCase))
						return true;
				}
				return false;
			});
		}
	}
}

public enum InfoIconType
{
	FontIcon,
	PathIcon
}

[GeneratedBindableCustomProperty]
public partial class InfoItem
{
	public string? Label { get; set; }
	public string? Value { get; set; }
	public string? Glyph { get; set; }
	public Geometry? PathData { get; set; }
	public InfoIconType IconType { get; set; }
	public bool IsFontIcon => IconType == InfoIconType.FontIcon;
	public bool IsPathIcon => IconType == InfoIconType.PathIcon;
	public bool IsHyperlink { get; set; } = false;
	public string? Hyperlink { get; set; }
}

