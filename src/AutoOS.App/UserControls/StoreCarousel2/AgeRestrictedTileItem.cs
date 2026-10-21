namespace AutoOS.App.UserControls.StoreCarousel2;

public class AgeRestrictedTileItem : IImageItem
{
	private readonly IImageItem? _imageItem;

	private AgeRestrictedTileItem(IImageItem? imageItem)
	{
		_imageItem = imageItem;
	}

	public bool IsAgeBlocked { get; private set; }

	public object? Data => _imageItem?.Data;
	public Uri? ImageUri => _imageItem?.ImageUri;

	public static AgeRestrictedTileItem Create(IEnumerable? items, bool isAgeBlocked)
	{
		return new AgeRestrictedTileItem(FirstImageItem(items)) { IsAgeBlocked = isAgeBlocked };
	}

	private static IImageItem? FirstImageItem(IEnumerable? items)
	{
		if (items == null)
		{
			return null;
		}

		foreach (object item in items)
		{
			if (item is IImageItem imageItem and not VideoPlayerSource)
			{
				return imageItem;
			}
		}

		return null;
	}
}
