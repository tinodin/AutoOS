namespace AutoOS.App.Services;

public interface IStatusCenterService
{
	void Attach(StatusCenterButton statusCenterButton);

	StatusCenterItem Begin(string header, string glyph);

	void Complete(StatusCenterItem card, ReturnResult result, string header, string? subHeader = null);
}
