using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AutoOS.App.Services;

public sealed class StatusCenterService : IStatusCenterService
{
	private StatusCenterButton? _statusCenterButton;

	public void Attach(StatusCenterButton statusCenterButton)
	{
		_statusCenterButton = statusCenterButton;
	}

	public StatusCenterItem Begin(string header, string glyph, bool canProvideProgress = false, long itemsCount = 0, Func<StatusCenterItem, StatusCenterItemProgressModel, (string Header, string SubHeader, string Message)>? update = null)
	{
		StatusCenterItem card = _statusCenterButton!.AddCard(ReturnResult.InProgress, header, header, string.Empty, StatusCenterItemIconKind.InstallFont, canProvideProgress, itemsCount, update: update);
		card.IconSource = new FontIconSource { Glyph = glyph, FontFamily = new FontFamily("Segoe Fluent Icons"), FontSize = 16 };
		card.IsCancelable = false;
		card.IsPausable = false;
		return card;
	}

	public void Complete(StatusCenterItem card, ReturnResult result, string header, string? subHeader = null)
	{
		_statusCenterButton!.RemoveItem(card);
		_statusCenterButton.AddCard(result, header, subHeader ?? header, string.Empty, result is ReturnResult.Failed ? StatusCenterItemIconKind.Error : StatusCenterItemIconKind.Successful, canProvideProgress: false);
	}
}
