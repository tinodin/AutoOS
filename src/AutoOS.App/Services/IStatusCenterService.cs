namespace AutoOS.App.Services;

public interface IStatusCenterService
{
	void Attach(StatusCenterButton statusCenterButton);

	StatusCenterItem Begin(string header, string glyph, bool canProvideProgress = false, long itemsCount = 0, Func<StatusCenterItem, StatusCenterItemProgressModel, (string Header, string SubHeader, string Message)>? update = null);

	void Complete(StatusCenterItem card, ReturnResult result, string header, string? subHeader = null);
}
