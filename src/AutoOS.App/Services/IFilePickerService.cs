namespace AutoOS.App.Services;

public interface IFilePickerService
{
	Task<string?> PickSingleFileAsync(string filterName, string[] extensions, string? initialDirectory = null, bool allowAllFiles = false);

	Task<string?> PickSaveFileAsync(string filterName, string[] extensions, string? suggestedFileName = null);
}
