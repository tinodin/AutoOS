using System.Runtime.InteropServices;
using DevWinUI;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using WinRT.Interop;

namespace AutoOS.Core.Helpers.Picker;

public partial class FilePicker
{
	private IntPtr hwnd;
	public FilePicker(IntPtr hwnd)
	{
		this.hwnd = hwnd;
	}
	public FilePicker(Window window) : this(WindowNative.GetWindowHandle(window)) { }
	public PickerOptions Options { get; set; } = PickerOptions.None;

	public bool ShowDetailedExtension { get; set; } = true;
	public string? CommitButtonText { get; set; }
	public string? SuggestedFileName { get; set; }
	public string? DefaultFileExtension { get; set; }
	public string? InitialDirectory { get; set; }
	public Microsoft.Windows.Storage.Pickers.PickerLocationId SuggestedStartLocation { get; set; } = Microsoft.Windows.Storage.Pickers.PickerLocationId.Unspecified;
	public string? Title { get; set; }
	public Dictionary<string, IList<string>> FileTypeChoices { get; set; } = [];
	public bool ShowAllFilesOption { get; set; } = true;

	/// <summary>
	/// picks a single file.
	/// </summary>
	/// <returns>Returns the path of the selected file or null if no file was selected.</returns>
	public string? PickSingleFile()
	{
		List<string> files = OpenFileDialog(false);
		return files.Count > 0 ? files[0] : null;
	}

	/// <summary>
	/// Asynchronously picks single file.
	/// </summary>
	/// <returns>Returns the selected file as a StorageFile or null if no file was selected.</returns>
	public async Task<StorageFile?> PickSingleFileAsync()
	{
		List<string> files = OpenFileDialog(false);
		return files.Count > 0 ? await StorageFile.GetFileFromPathAsync(files[0]) : null;
	}

	/// <summary>
	/// picks multiple files.
	/// </summary>
	/// <returns>Returns the path of the selected files or null if no file was selected.</returns>
	public List<string> PickMultipleFiles()
	{
		return OpenFileDialog(true);
	}

	/// <summary>
	/// Asynchronously picks multiple files.
	/// </summary>
	/// <returns>Returns A list of StorageFile selected by the user.</returns>
	public async Task<List<StorageFile>> PickMultipleFilesAsync()
	{
		List<string> filePaths = OpenFileDialog(true);
		var storageFiles = new List<StorageFile>();
		foreach (string path in filePaths)
		{
			storageFiles.Add(await StorageFile.GetFileFromPathAsync(path));
		}
		return storageFiles;
	}

	private unsafe List<string> OpenFileDialog(bool allowMultiple)
	{
		int hr = PInvoke.CoCreateInstance<IFileOpenDialog>(
							typeof(FileOpenDialog).GUID,
							null,
							CLSCTX.CLSCTX_INPROC_SERVER,
							out IFileOpenDialog* fod);
		if (hr < 0)
		{
			Marshal.ThrowExceptionForHR(hr);
		}

		IntPtr dialogPtr = (IntPtr)fod;
		var dialog = (IFileOpenDialog*)dialogPtr;

		try
		{
			if (!string.IsNullOrEmpty(Title))
			{
				dialog->SetTitle(Title);
			}

			if (!string.IsNullOrEmpty(CommitButtonText))
			{
				dialog->SetOkButtonLabel(CommitButtonText);
			}

			if (SuggestedStartLocation != Microsoft.Windows.Storage.Pickers.PickerLocationId.Unspecified)
			{
				InitialDirectory = PathHelper.GetKnownFolderPath(SuggestedStartLocation);
			}

			if (!string.IsNullOrEmpty(InitialDirectory))
			{
				PInvoke.SHCreateItemFromParsingName(InitialDirectory, null, typeof(IShellItem).GUID, out void* ppv);
				IShellItem* psi = (IShellItem*)ppv;

				dialog->SetFolder(psi);
			}

			if (!string.IsNullOrEmpty(SuggestedFileName))
			{
				dialog->SetFileName(SuggestedFileName);
			}

			var filters = new List<COMDLG_FILTERSPEC>();

			if (ShowAllFilesOption)
			{
				filters.Add(new COMDLG_FILTERSPEC { pszName = (char*)Marshal.StringToHGlobalUni("All Files (*.*)"), pszSpec = (char*)Marshal.StringToHGlobalUni("*.*") });
			}

			foreach (KeyValuePair<string, IList<string>> kvp in FileTypeChoices)
			{
				string displayName = kvp.Key;

				if (ShowDetailedExtension)
				{
					string extensions = string.Join(", ", kvp.Value);
					displayName = $"{kvp.Key} ({extensions})";
				}

				string spec = string.Join(";", kvp.Value);
				filters.Add(new COMDLG_FILTERSPEC { pszName = (char*)Marshal.StringToHGlobalUni(displayName), pszSpec = (char*)Marshal.StringToHGlobalUni(spec) });
			}

			dialog->SetFileTypes(filters.ToArray());

			if (!string.IsNullOrEmpty(DefaultFileExtension) && FileTypeChoices.ContainsKey(DefaultFileExtension))
			{
				int defaultIndex = new List<string>(FileTypeChoices.Keys).IndexOf(DefaultFileExtension) + (ShowAllFilesOption ? 1 : 0);
				dialog->SetFileTypeIndex((uint)(defaultIndex + 1));
			}

			if (allowMultiple)
			{
				Options |= PickerOptions.FOS_ALLOWMULTISELECT;
			}

			dialog->SetOptions(PickerHelper.MapPickerOptionsToFOS(Options));

			try
			{
				dialog->Show(new HWND(hwnd));
			}
			catch (Exception ex) when ((uint)(ex.HResult) == 0x800704C7) // ERROR_CANCELLED
			{
				// User canceled the dialog, return an empty list
				return [];
			}

			List<string> filePaths = [];

			if (allowMultiple)
			{
				IShellItemArray* resultsPtr = null;
				dialog->GetResults((IShellItemArray**)(&resultsPtr));

				if (resultsPtr != null)
				{
					resultsPtr->GetCount(out uint count);

					for (uint i = 0; i < count; i++)
					{
						IShellItem* itemPtr = null;
						resultsPtr->GetItemAt(i, (IShellItem**)(&itemPtr));
						if (itemPtr != null)
						{
							itemPtr->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out PWSTR filePath);
							if (filePath != null)
							{
								filePaths.Add(filePath.ToString());
							}
						}
					}
				}
			}
			else
			{
				IShellItem* resultsPtr = null;
				dialog->GetResult((IShellItem**)(&resultsPtr));

				if (resultsPtr != null)
				{
					resultsPtr->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out PWSTR filePath);
					if (filePath != null)
					{
						filePaths.Add(filePath.ToString());
					}
				}
			}

			return filePaths;
		}
		finally
		{
			dialog->Close(new HRESULT(0));
			dialog->Release();
		}
	}
}
