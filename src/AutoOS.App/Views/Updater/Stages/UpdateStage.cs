namespace AutoOS.App.Views.Updater.Stages;

public static class UpdateStage
{
	public static List<(string Title, Func<Task> Action, Func<bool> Condition)> UpdateActions(UpdateDialog dialog)
	{
		var actions = new List<(string Title, Func<Task> Action, Func<bool> Condition)>()
		{
		};
		
		return actions;
	}
}
