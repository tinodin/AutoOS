namespace AutoOS.App.Data.Models.Bios;

public static class Validation
{
	public static string[] GetErrors(State state, bool hasOptions)
	{
		if (hasOptions)
			return state.SelectedOption == null ? ["No option selected"] : [];

		return string.IsNullOrWhiteSpace(state.Value) ? ["Value is empty"] : [];
	}
}
