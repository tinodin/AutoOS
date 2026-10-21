namespace AutoOS.App.UserControls;

public class ViewDisplayedTrackerBehavior : OnScreenBehaviorBase
{
	private bool _keepTrackingVisibilityChanges;

	public bool KeepTrackingVisibilityChanges
	{
		get => _keepTrackingVisibilityChanges;
		set => _keepTrackingVisibilityChanges = value;
	}

	protected override void OnIsOnScreenChanged(bool isOnScreen)
	{
		if (isOnScreen && !KeepTrackingVisibilityChanges)
		{
			StopTracking();
		}
	}
}
