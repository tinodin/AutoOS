using AutoOS.Core.Helpers.Picker;
using Windows.Storage;

namespace AutoOS.App.Services;

public sealed class FilePickerService : IFilePickerService
{
	public async Task<string?> PickSingleFileAsync(string filterName, string[] extensions, string? initialDirectory = null, bool allowAllFiles = false)
	{
		var picker = new FilePicker(App.MainWindow)
		{
			ShowAllFilesOption = allowAllFiles,
			InitialDirectory = initialDirectory
		};
		picker.FileTypeChoices.Add(filterName, extensions);

		StorageFile? file = await picker.PickSingleFileAsync();
		return file?.Path;
	}

	public async Task<string?> PickSaveFileAsync(string filterName, string[] extensions, string? suggestedFileName = null)
	{
		var picker = new SavePicker(App.MainWindow)
		{
			ShowAllFilesOption = false,
			SuggestedFileName = suggestedFileName
		};
		picker.FileTypeChoices.Add(filterName, extensions);

		StorageFile? file = await picker.PickSaveFileAsync();
		if (file == null)
			return null;

		string primaryExtension = extensions.FirstOrDefault()?.Replace("*", string.Empty, StringComparison.Ordinal) ?? string.Empty;
		return file.Path.EndsWith(primaryExtension, StringComparison.OrdinalIgnoreCase) ? file.Path : $"{file.Path}{primaryExtension}";
	}
}
