using xTile.Dimensions;
using xTile.ObjectModel;

namespace xTile.Tiles;

public class TileSheet : DescribedComponent
{
	private Map m_map;

	private string m_imageSource;

	private Size m_sheetSize;

	private Size m_tileSize;

	private Size m_margin;

	private Size m_spacing;

	private readonly TileIndexPropertyAccessor m_tileIndexPropertyAccessor;

	public Map Map => m_map;

	public string ImageSource
	{
		get
		{
			return m_imageSource;
		}
		set
		{
			m_imageSource = value;
		}
	}

	public Size SheetSize
	{
		get
		{
			return m_sheetSize;
		}
		set
		{
			m_sheetSize = value;
		}
	}

	public int SheetWidth
	{
		get
		{
			return m_sheetSize.Width;
		}
		set
		{
			m_sheetSize.Width = value;
		}
	}

	public int SheetHeight
	{
		get
		{
			return m_sheetSize.Height;
		}
		set
		{
			m_sheetSize.Height = value;
		}
	}

	public Size TileSize
	{
		get
		{
			return m_tileSize;
		}
		set
		{
			m_tileSize = value;
		}
	}

	public int TileWidth
	{
		get
		{
			return m_tileSize.Width;
		}
		set
		{
			m_tileSize.Width = value;
		}
	}

	public int TileHeight
	{
		get
		{
			return m_tileSize.Height;
		}
		set
		{
			m_tileSize.Height = value;
		}
	}

	public Size Margin
	{
		get
		{
			return m_margin;
		}
		set
		{
			m_margin = value;
		}
	}

	public int MarginWidth
	{
		get
		{
			return m_margin.Width;
		}
		set
		{
			m_margin.Width = value;
		}
	}

	public int MarginHeight
	{
		get
		{
			return m_margin.Height;
		}
		set
		{
			m_margin.Height = value;
		}
	}

	public Size Spacing
	{
		get
		{
			return m_spacing;
		}
		set
		{
			m_spacing = value;
		}
	}

	public int SpacingWidth
	{
		get
		{
			return m_spacing.Width;
		}
		set
		{
			m_spacing.Width = value;
		}
	}

	public int SpacingHeight
	{
		get
		{
			return m_spacing.Height;
		}
		set
		{
			m_spacing.Height = value;
		}
	}

	public int TileCount => m_sheetSize.Width * m_sheetSize.Height;

	public TileIndexPropertyAccessor TileIndexProperties => m_tileIndexPropertyAccessor;

	public TileSheet(Map map, string imageSource, Size sheetSize, Size tileSize)
	{
		m_map = map;
		m_imageSource = imageSource;
		m_sheetSize = sheetSize;
		m_tileSize = tileSize;
		m_margin = (m_spacing = Size.Zero);
		m_tileIndexPropertyAccessor = new TileIndexPropertyAccessor(this);
	}

	public TileSheet(string id, Map map, string imageSource, Size sheetSize, Size tileSize)
		: base(id)
	{
		m_map = map;
		m_imageSource = imageSource;
		m_sheetSize = sheetSize;
		m_tileSize = tileSize;
		m_margin = (m_spacing = Size.Zero);
		m_tileIndexPropertyAccessor = new TileIndexPropertyAccessor(this);
	}

	public Rectangle GetTileImageBounds(int tileIndex)
	{
		return new Rectangle(16 * (tileIndex % m_sheetSize.Width), 16 * (tileIndex / m_sheetSize.Width), 16, 16);
	}

	public int GetTileIndex(Location pixelLocation)
	{
		int num = (pixelLocation.X - m_margin.Width) / (m_tileSize.Width + m_spacing.Width);
		return (pixelLocation.Y - m_margin.Height) / (m_tileSize.Height + m_spacing.Height) * m_sheetSize.Width + num;
	}
}
