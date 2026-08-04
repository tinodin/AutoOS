using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI.Controls;

namespace AutoOS.App.Extensions;

public static class CommandBarExtensions
{
    private static readonly ConditionalWeakTable<CommandBar, CommandBarState> _stateMap = [];

    private sealed class CommandBarState
    {
        public Dictionary<AppBarElementContainer, Thickness> Margins { get; } = [];
        public List<(UIElement Element, long Token)> VisibilityTokens { get; } = [];
        public Windows.Foundation.Collections.VectorChangedEventHandler<ICommandBarElement> VectorChangedHandler { get; set; }
        public SizeChangedEventHandler SizeChangedHandler { get; set; }
        public bool RefreshQueued { get; set; }
        public bool RefreshDirty { get; set; }
    }

    public static readonly DependencyProperty ApplyOverflowIndentProperty =
        DependencyProperty.RegisterAttached(
            "ApplyOverflowIndent",
            typeof(bool),
            typeof(CommandBarExtensions),
            new PropertyMetadata(false, OnApplyOverflowIndentChanged));

    public static bool GetApplyOverflowIndent(DependencyObject obj)
    {
        return (bool)obj.GetValue(ApplyOverflowIndentProperty);
    }

    public static void SetApplyOverflowIndent(DependencyObject obj, bool value)
    {
        obj.SetValue(ApplyOverflowIndentProperty, value);
    }

    public static readonly DependencyProperty CommandAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "CommandAlignment",
            typeof(HorizontalAlignment),
            typeof(CommandBarExtensions),
            new PropertyMetadata(HorizontalAlignment.Left, OnCommandAlignmentChanged));

    public static HorizontalAlignment GetCommandAlignment(DependencyObject obj)
    {
        return (HorizontalAlignment)obj.GetValue(CommandAlignmentProperty);
    }

    public static void SetCommandAlignment(DependencyObject obj, HorizontalAlignment value)
    {
        obj.SetValue(CommandAlignmentProperty, value);
    }

    public static readonly DependencyProperty OverflowButtonAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "OverflowButtonAlignment",
            typeof(HorizontalAlignment),
            typeof(CommandBarExtensions),
            new PropertyMetadata(HorizontalAlignment.Right, OnOverflowButtonAlignmentChanged));

    public static HorizontalAlignment GetOverflowButtonAlignment(DependencyObject obj)
    {
        return (HorizontalAlignment)obj.GetValue(OverflowButtonAlignmentProperty);
    }

    public static void SetOverflowButtonAlignment(DependencyObject obj, HorizontalAlignment value)
    {
        obj.SetValue(OverflowButtonAlignmentProperty, value);
    }

    private static void ForEachCommandBar(DependencyObject dependencyObject, Action<CommandBar> action)
    {
        if (dependencyObject is CommandBar commandBar)
        {
            action(commandBar);
        }
        else if (dependencyObject is TabbedCommandBar tabbedCommandBar)
        {
            if (tabbedCommandBar.IsLoaded)
            {
                foreach (TabbedCommandBarItem item in tabbedCommandBar.MenuItems.OfType<TabbedCommandBarItem>())
                    action(item);
            }
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (sender, args) =>
                {
                    tabbedCommandBar.Loaded -= loadedHandler;
                    foreach (TabbedCommandBarItem item in tabbedCommandBar.MenuItems.OfType<TabbedCommandBarItem>())
                        action(item);
                };
                tabbedCommandBar.Loaded += loadedHandler;
            }
        }
    }

    private static void OnApplyOverflowIndentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        bool newValue = (bool)args.NewValue;

        ForEachCommandBar(dependencyObject, commandBar =>
        {
            if (newValue)
            {
                commandBar.Opening += CommandBar_Opening;
                commandBar.Closed += CommandBar_Closed;

                if (commandBar.IsOpen)
                    ApplyOverflowContainerMargins(commandBar);

                AttachSizeChangedTracking(commandBar);
                AttachVisibilityTracking(commandBar);

                if (commandBar.IsLoaded)
                {
                    UpdateCommandAlignment(commandBar, GetCommandAlignment(dependencyObject));
                    UpdateOverflowButtonAlignment(commandBar, GetOverflowButtonAlignment(dependencyObject));
                }
                else
                {
                    RoutedEventHandler loadedHandler = null;
                    loadedHandler = (sender, eventArgs) =>
                    {
                        commandBar.Loaded -= loadedHandler;
                        UpdateCommandAlignment(commandBar, GetCommandAlignment(dependencyObject));
                        UpdateOverflowButtonAlignment(commandBar, GetOverflowButtonAlignment(dependencyObject));
                        AttachSizeChangedTracking(commandBar);
                        AttachVisibilityTracking(commandBar);
                    };
                    commandBar.Loaded += loadedHandler;
                }
            }
            else
            {
                commandBar.Opening -= CommandBar_Opening;
                commandBar.Closed -= CommandBar_Closed;
                DetachSizeChangedTracking(commandBar);
                DetachVisibilityTracking(commandBar);
                RestoreOverflowContainerMargins(commandBar);
            }
        });
    }

    private static void OnCommandAlignmentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var alignment = (HorizontalAlignment)args.NewValue;
        ForEachCommandBar(dependencyObject, commandBar =>
        {
            if (commandBar.IsLoaded)
                UpdateCommandAlignment(commandBar, alignment);
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (sender, eventArgs) =>
                {
                    commandBar.Loaded -= loadedHandler;
                    UpdateCommandAlignment(commandBar, alignment);
                };
                commandBar.Loaded += loadedHandler;
            }
        });
    }

    private static void OnOverflowButtonAlignmentChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var alignment = (HorizontalAlignment)args.NewValue;
        ForEachCommandBar(dependencyObject, commandBar =>
        {
            if (commandBar.IsLoaded)
                UpdateOverflowButtonAlignment(commandBar, alignment);
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (sender, eventArgs) =>
                {
                    commandBar.Loaded -= loadedHandler;
                    UpdateOverflowButtonAlignment(commandBar, alignment);
                };
                commandBar.Loaded += loadedHandler;
            }
        });
    }

    private static void AttachVisibilityTracking(CommandBar commandBar)
    {
		CommandBarState state = _stateMap.GetOrCreateValue(commandBar);
        if (state.VectorChangedHandler is null)
        {
            state.VectorChangedHandler = (_, _) =>
            {
                HookVisibilityCallbacks(commandBar);
                QueueOverflowRefresh(commandBar);
            };
            commandBar.PrimaryCommands.VectorChanged += state.VectorChangedHandler;
        }

        HookVisibilityCallbacks(commandBar);
    }

    private static void DetachVisibilityTracking(CommandBar commandBar)
    {
        if (!_stateMap.TryGetValue(commandBar, out CommandBarState? state))
            return;

        if (state.VectorChangedHandler is not null)
        {
            commandBar.PrimaryCommands.VectorChanged -= state.VectorChangedHandler;
            state.VectorChangedHandler = null;
        }

        UnhookVisibilityCallbacks(state);
    }

    private static void AttachSizeChangedTracking(CommandBar commandBar)
    {
		CommandBarState state = _stateMap.GetOrCreateValue(commandBar);
        if (state.SizeChangedHandler is null)
        {
            state.SizeChangedHandler = (_, _) => QueueOverflowRefresh(commandBar);
            commandBar.SizeChanged += state.SizeChangedHandler;
        }
    }

    private static void DetachSizeChangedTracking(CommandBar commandBar)
    {
        if (!_stateMap.TryGetValue(commandBar, out CommandBarState? state))
            return;

        if (state.SizeChangedHandler is not null)
        {
            commandBar.SizeChanged -= state.SizeChangedHandler;
            state.SizeChangedHandler = null;
        }
    }

    private static void HookVisibilityCallbacks(CommandBar commandBar)
    {
		CommandBarState state = _stateMap.GetOrCreateValue(commandBar);
        UnhookVisibilityCallbacks(state);

        foreach (UIElement element in commandBar.PrimaryCommands.OfType<UIElement>())
        {
            long token = element.RegisterPropertyChangedCallback(
                UIElement.VisibilityProperty,
                (_, _) => QueueOverflowRefresh(commandBar));
            state.VisibilityTokens.Add((element, token));
        }
    }

    private static void UnhookVisibilityCallbacks(CommandBarState state)
    {
        foreach ((UIElement? element, long token) in state.VisibilityTokens)
            element.UnregisterPropertyChangedCallback(UIElement.VisibilityProperty, token);
        state.VisibilityTokens.Clear();
    }

    private static void QueueOverflowRefresh(CommandBar commandBar)
    {
		CommandBarState state = _stateMap.GetOrCreateValue(commandBar);
        if (state.RefreshQueued)
        {
            state.RefreshDirty = true;
            return;
        }

        state.RefreshQueued = true;
        state.RefreshDirty = false;
        if (!commandBar.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    RefreshOverflow(commandBar);
                    while (state.RefreshDirty)
                    {
                        state.RefreshDirty = false;
                        RefreshOverflow(commandBar);
                    }
                }
                finally
                {
                    state.RefreshQueued = false;
                    if (state.RefreshDirty)
                        QueueOverflowRefresh(commandBar);
                }
            }))
        {
            state.RefreshQueued = false;
        }
    }

    private static void RefreshOverflow(CommandBar commandBar)
    {
        if (!commandBar.IsLoaded || !commandBar.IsDynamicOverflowEnabled)
            return;

        commandBar.IsDynamicOverflowEnabled = false;
        commandBar.IsDynamicOverflowEnabled = true;
        commandBar.UpdateLayout();

        bool show = HasOverflowContent(commandBar);
        SetOverflowButtonVisibility(commandBar, show);
        commandBar.UpdateLayout();

        bool showAfterLayout = HasOverflowContent(commandBar);
        if (showAfterLayout != show)
            SetOverflowButtonVisibility(commandBar, showAfterLayout);
    }

    private static bool HasOverflowContent(CommandBar commandBar)
    {
        if (commandBar.SecondaryCommands.Count > 0)
            return true;

        foreach (ICommandBarElement command in commandBar.PrimaryCommands)
        {
            if (command.IsInOverflow)
                return true;
        }

        return false;
    }

    private static void SetOverflowButtonVisibility(CommandBar commandBar, bool show)
    {
		CommandBarOverflowButtonVisibility target = show
            ? CommandBarOverflowButtonVisibility.Visible
            : CommandBarOverflowButtonVisibility.Auto;

        if (commandBar.OverflowButtonVisibility == target && show)
            commandBar.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;

        commandBar.OverflowButtonVisibility = target;
    }

    private static void UpdateCommandAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			ItemsControl primaryItemsControl = FindVisualChild<ItemsControl>(commandBar, "PrimaryItemsControl");
            primaryItemsControl?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static void UpdateOverflowButtonAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			Button moreButton = FindVisualChild<Button>(commandBar, "MoreButton");
            moreButton?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
			DependencyObject child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is T matched && (child as FrameworkElement)?.Name == childName)
            {
                return matched;
            }
            T childOfChild = FindVisualChild<T>(child, childName);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }

    private static void CommandBar_Opening(object sender, object eventArgs)
    {
        if (sender is CommandBar commandBar)
            ApplyOverflowContainerMargins(commandBar);
    }

    private static void CommandBar_Closed(object sender, object eventArgs)
    {
        if (sender is CommandBar commandBar)
            RestoreOverflowContainerMargins(commandBar);
    }

    private static void ApplyOverflowContainerMargins(CommandBar commandBar)
    {
		Dictionary<AppBarElementContainer, Thickness> margins = _stateMap.GetOrCreateValue(commandBar).Margins;

        foreach (AppBarElementContainer container in commandBar.PrimaryCommands.OfType<AppBarElementContainer>())
        {
            if (container.IsInOverflow && margins.TryAdd(container, container.Margin))
                container.Margin = new Thickness(32, 0, 32, 4);
        }

        foreach (AppBarElementContainer container in commandBar.SecondaryCommands.OfType<AppBarElementContainer>())
        {
            if (margins.TryAdd(container, container.Margin))
                container.Margin = new Thickness(32, 0, 32, 4);
        }
    }

    private static void RestoreOverflowContainerMargins(CommandBar commandBar)
    {
        if (_stateMap.TryGetValue(commandBar, out CommandBarState? state))
        {
            foreach ((AppBarElementContainer? container, Thickness margin) in state.Margins)
                container.Margin = margin;
            state.Margins.Clear();
        }
    }
}
