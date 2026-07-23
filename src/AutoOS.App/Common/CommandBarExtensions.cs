using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI.Controls;

namespace AutoOS.Common;

public static class CommandBarExtensions
{
    private static readonly ConditionalWeakTable<CommandBar, Dictionary<AppBarElementContainer, Thickness>> _marginsMap = [];

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
                foreach (var item in tabbedCommandBar.MenuItems.OfType<TabbedCommandBarItem>())
                    action(item);
            }
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (sender, args) =>
                {
                    tabbedCommandBar.Loaded -= loadedHandler;
                    foreach (var item in tabbedCommandBar.MenuItems.OfType<TabbedCommandBarItem>())
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
                    };
                    commandBar.Loaded += loadedHandler;
                }
            }
            else
            {
                commandBar.Opening -= CommandBar_Opening;
                commandBar.Closed -= CommandBar_Closed;
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

    private static void UpdateCommandAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			var primaryItemsControl = FindVisualChild<ItemsControl>(commandBar, "PrimaryItemsControl");
			primaryItemsControl?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static void UpdateOverflowButtonAlignment(CommandBar commandBar, HorizontalAlignment alignment)
    {
        try
        {
            commandBar.ApplyTemplate();
			var moreButton = FindVisualChild<Button>(commandBar, "MoreButton");
			moreButton?.HorizontalAlignment = alignment;
        }
        catch { }
    }

    private static T FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int index = 0; index < count; index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is T matched && (child as FrameworkElement)?.Name == childName)
            {
                return matched;
            }
            var childOfChild = FindVisualChild<T>(child, childName);
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
        var margins = _marginsMap.GetOrCreateValue(commandBar);

        foreach (var container in commandBar.PrimaryCommands.OfType<AppBarElementContainer>())
        {
            if (container.IsInOverflow && margins.TryAdd(container, container.Margin))
                container.Margin = new Thickness(32, 0, 0, 0);
        }

        foreach (var container in commandBar.SecondaryCommands.OfType<AppBarElementContainer>())
        {
            if (margins.TryAdd(container, container.Margin))
                container.Margin = new Thickness(32, 0, 0, 0);
        }
    }

    private static void RestoreOverflowContainerMargins(CommandBar commandBar)
    {
        if (_marginsMap.TryGetValue(commandBar, out var margins))
        {
            foreach (var (container, margin) in margins)
                container.Margin = margin;
            margins.Clear();
        }
    }
}
