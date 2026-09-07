using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace AutoOS.App.Data.Commands;

public sealed partial class CopyTextCommand : ICommand
{
	public event EventHandler? CanExecuteChanged
	{
		add { }
		remove { }
	}

	public bool CanExecute(object? parameter) => parameter is string s && !string.IsNullOrEmpty(s);

	public void Execute(object? parameter)
	{
		if (parameter is not string text)
			return;

		DataPackage dataPackage = new();
		dataPackage.SetText(text);
		Clipboard.SetContent(dataPackage);
		Clipboard.Flush();
	}
}
