using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace AutoOS.App.UserControls.StoreCarousel2;

public abstract class ChangePropertyAction<TTarget, TValue> : DependencyObject, IAction
{
	public static readonly DependencyProperty TargetObjectProperty =
		DependencyProperty.Register(nameof(TargetObject), typeof(TTarget), typeof(ChangePropertyAction<TTarget, TValue>), new PropertyMetadata(null));

	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(nameof(Value), typeof(TValue), typeof(ChangePropertyAction<TTarget, TValue>), new PropertyMetadata(default(TValue)));

	public TTarget TargetObject
	{
		get => (TTarget)GetValue(TargetObjectProperty);
		set => SetValue(TargetObjectProperty, value);
	}

	public TValue Value
	{
		get => (TValue)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public object? Execute(object sender, object parameter)
	{
		if (TargetObject is TTarget target)
		{
			Change(target, Value);
		}

		return null;
	}

	protected abstract void Change(TTarget target, TValue value);
}
