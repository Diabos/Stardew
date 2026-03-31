using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using xTile.Dimensions;
using xTile.Display;
using xTile.ObjectModel;
using xTile.Tiles;

namespace xTile.Layers;

public class Layer : DescribedComponent
{
	public static int zoom = 4;

	private Map m_map;

	private ReadOnlyCollection<TileSheet> m_tileSheets;

	private Size m_layerSize;

	public static Size m_tileSize;

	private Tile[,] m_tiles;

	protected HashSet<int> _dirtyRows;

	protected int[,] _skipMap;

	private TileArray m_tileArray;

	private bool m_visible;

	public Map Map => m_map;

	public Size LayerSize
	{
		get
		{
			return m_layerSize;
		}
		set
		{
			if (m_layerSize == value)
			{
				return;
			}
			Tile[,] array = new Tile[value.Width, value.Height];
			int num = Math.Min(m_layerSize.Width, value.Width);
			int num2 = Math.Min(m_layerSize.Height, value.Height);
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					array[j, i] = m_tiles[j, i];
				}
			}
			m_tiles = array;
			m_tileArray = new TileArray(this, m_tiles);
			m_layerSize = value;
			m_map.UpdateDisplaySize();
		}
	}

	public int LayerWidth
	{
		get
		{
			return m_layerSize.Width;
		}
		set
		{
			LayerSize = new Size(value, m_layerSize.Height);
		}
	}

	public int LayerHeight
	{
		get
		{
			return m_layerSize.Height;
		}
		set
		{
			LayerSize = new Size(m_layerSize.Width, value);
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

	public Size DisplaySize => new Size(m_layerSize.Width * m_tileSize.Width, m_layerSize.Height * m_tileSize.Height);

	public int DisplayWidth => m_layerSize.Width * m_tileSize.Width;

	public int DisplayHeight => m_layerSize.Height * m_tileSize.Height;

	public bool Visible
	{
		get
		{
			return m_visible;
		}
		set
		{
			m_visible = value;
		}
	}

	public TileArray Tiles => m_tileArray;

	public event LayerEventHandler BeforeDraw;

	public event LayerEventHandler AfterDraw;

	public Layer(string id, Map map, Size layerSize, Size tileSize)
		: base(id)
	{
		m_map = map;
		m_tileSheets = map.TileSheets;
		m_layerSize = layerSize;
		m_tileSize = tileSize;
		m_tiles = new Tile[layerSize.Width, layerSize.Height];
		m_tileArray = new TileArray(this, m_tiles);
		m_visible = true;
		_dirtyRows = new HashSet<int>();
	}

	public void MarkRowDirty(int y)
	{
		_dirtyRows.Add(y);
	}

	public bool DependsOnTileSheet(TileSheet tileSheet)
	{
		for (int i = 0; i < m_layerSize.Height; i++)
		{
			for (int j = 0; j < m_layerSize.Width; j++)
			{
				Tile tile = m_tiles[j, i];
				if (tile != null && tile.DependsOnTileSheet(tileSheet))
				{
					return true;
				}
			}
		}
		return false;
	}

	public Location GetTileLocation(Location layerDisplayLocation)
	{
		return new Location(layerDisplayLocation.X / m_tileSize.Width, layerDisplayLocation.Y / m_tileSize.Height);
	}

	public bool IsValidTileLocation(Location tileLocation)
	{
		if (tileLocation.X >= 0 && tileLocation.X < m_layerSize.Width && tileLocation.Y >= 0)
		{
			return tileLocation.Y < m_layerSize.Height;
		}
		return false;
	}

	public bool IsValidTileLocation(int tileX, int tileY)
	{
		if (tileX >= 0 && tileX < m_layerSize.Width && tileY >= 0)
		{
			return tileY < m_layerSize.Height;
		}
		return false;
	}

	public Rectangle ConvertMapToLayerViewport(Rectangle mapViewport)
	{
		return new Rectangle(ConvertMapToLayerLocation(mapViewport.Location, mapViewport.Size), mapViewport.Size);
	}

	public Location ConvertMapToLayerLocation(Location mapDisplayLocation, Size viewportSize)
	{
		int x = mapDisplayLocation.X / m_tileSize.Width;
		int y = mapDisplayLocation.Y / m_tileSize.Height;
		return new Location(x, y);
	}

	public Location ConvertLayerToMapLocation(Location layerDisplayLocation, Size viewportSize)
	{
		Size displaySize = m_map.DisplaySize;
		Size displaySize2 = DisplaySize;
		return new Location(layerDisplayLocation.X * (displaySize.Width - viewportSize.Width) / (displaySize2.Width - viewportSize.Width), layerDisplayLocation.Y * (displaySize.Height - viewportSize.Height) / (displaySize2.Height - viewportSize.Height));
	}

	public Rectangle GetTileDisplayRectangle(Rectangle mapViewport, Location tileLocation)
	{
		Location location = ConvertMapToLayerLocation(mapViewport.Location, mapViewport.Size);
		return new Rectangle(new Location(tileLocation.X * m_tileSize.Width, tileLocation.Y * m_tileSize.Height) - location, m_tileSize);
	}

	public Tile PickTile(Location mapDisplayLocation, Size viewportSize)
	{
		Location tileLocation = ConvertMapToLayerLocation(mapDisplayLocation, viewportSize);
		if (IsValidTileLocation(tileLocation))
		{
			return m_tiles[tileLocation.X, tileLocation.Y];
		}
		return null;
	}

	public void RemoveTileSheetDependency(TileSheet tileSheet)
	{
		for (int i = 0; i < m_layerSize.Height; i++)
		{
			for (int j = 0; j < m_layerSize.Width; j++)
			{
				Tile tile = m_tiles[j, i];
				if (tile != null && tile.DependsOnTileSheet(tileSheet))
				{
					m_tiles[j, i] = null;
				}
			}
		}
	}

	public void Draw(IDisplayDevice displayDevice, Rectangle mapViewport, Location displayOffset, bool wrapAround, int pixelZoom, float sort_offset = 0f)
	{
		zoom = pixelZoom;
		if (wrapAround)
		{
			DrawWrapped(displayDevice, mapViewport, displayOffset);
		}
		else
		{
			DrawNormal(displayDevice, mapViewport, displayOffset, pixelZoom, sort_offset);
		}
	}

	private int Wrap(int value, int span)
	{
		value %= span;
		if (value < 0)
		{
			value += span;
		}
		return value;
	}

	protected void _RebakeRow(int y)
	{
		int num = -1;
		for (int num2 = LayerWidth - 1; num2 >= 0; num2--)
		{
			if (num >= 0)
			{
				num++;
			}
			_skipMap[num2, y] = num;
			if (Tiles[num2, y] != null)
			{
				num = 0;
			}
		}
	}

	private void DrawNormal(IDisplayDevice displayDevice, Rectangle mapViewport, Location displayOffset, int pixelZoom, float sort_offset = 0f)
	{
		if (this.BeforeDraw != null)
		{
			this.BeforeDraw(this, new LayerEventArgs(this, mapViewport));
		}
		int num = pixelZoom * 16;
		int num2 = pixelZoom * 16;
		Location location = new Location(Wrap(mapViewport.X, num), Wrap(mapViewport.Y, num2));
		int num3 = ((mapViewport.X >= 0) ? (mapViewport.X / num) : ((mapViewport.X - num + 1) / num));
		int num4 = ((mapViewport.Y >= 0) ? (mapViewport.Y / num2) : ((mapViewport.Y - num2 + 1) / num2));
		if (num3 < 0)
		{
			displayOffset.X -= num3 * num;
			num3 = 0;
		}
		if (num4 < 0)
		{
			displayOffset.Y -= num4 * num2;
			num4 = 0;
		}
		int num5 = 1 + (mapViewport.Size.Width - 1) / num;
		int num6 = 1 + (mapViewport.Size.Height - 1) / num2;
		if (location.X != 0)
		{
			num5++;
		}
		if (location.Y != 0)
		{
			num6++;
		}
		int num7 = Math.Min(num3 + num5, m_layerSize.Width);
		int num8 = Math.Min(num4 + num6, m_layerSize.Height);
		Location location2 = displayOffset - location;
		if (_skipMap == null)
		{
			_dirtyRows.Clear();
			_skipMap = new int[LayerWidth, LayerHeight];
			for (int i = 0; i < LayerHeight; i++)
			{
				_RebakeRow(i);
			}
		}
		else
		{
			foreach (int dirtyRow in _dirtyRows)
			{
				_RebakeRow(dirtyRow);
			}
			_dirtyRows.Clear();
		}
		for (int j = num4; j < num8; j++)
		{
			location2.X = displayOffset.X - location.X;
			int num9;
			for (int k = num3; k < num7; k += num9)
			{
				num9 = 1;
				num9 = _skipMap[k, j];
				Tile tile = m_tiles[k, j];
				if (tile != null)
				{
					float layerDepth = 0f;
					if (sort_offset >= 0f)
					{
						layerDepth = ((float)(j * (16 * pixelZoom) + 16 * pixelZoom) + sort_offset) / 10000f;
					}
					displayDevice.DrawTile(tile, location2, layerDepth);
				}
				location2.X += num * num9;
				if (num9 == -1)
				{
					break;
				}
			}
			location2.Y += num2;
		}
		if (this.AfterDraw != null)
		{
			this.AfterDraw(this, new LayerEventArgs(this, mapViewport));
		}
	}

	private void DrawWrapped(IDisplayDevice displayDevice, Rectangle mapViewport, Location displayOffset)
	{
		if (this.BeforeDraw != null)
		{
			this.BeforeDraw(this, new LayerEventArgs(this, mapViewport));
		}
		int width = m_tileSize.Width;
		int height = m_tileSize.Height;
		int width2 = m_layerSize.Width;
		int height2 = m_layerSize.Height;
		Location location = new Location(Wrap(mapViewport.X, width), Wrap(mapViewport.Y, height));
		int num = ((mapViewport.X >= 0) ? (mapViewport.X / width) : ((mapViewport.X - width + 1) / width));
		int num2 = ((mapViewport.Y >= 0) ? (mapViewport.Y / height) : ((mapViewport.Y - height + 1) / height));
		int num3 = 1 + (mapViewport.Size.Width - 1) / width;
		int num4 = 1 + (mapViewport.Size.Height - 1) / height;
		if (location.X != 0)
		{
			num3++;
		}
		if (location.Y != 0)
		{
			num4++;
		}
		int num5 = num + num3;
		int num6 = num2 + num4;
		Location location2 = displayOffset - location;
		for (int i = num2; i < num6; i++)
		{
			location2.X = displayOffset.X - location.X;
			for (int j = num; j < num5; j++)
			{
				Tile tile = m_tiles[Wrap(j, width2), Wrap(i, height2)];
				if (tile != null)
				{
					displayDevice.DrawTile(tile, location2, (float)(mapViewport.Y + location2.Y + 40) / 10000f);
				}
				location2.X += width;
			}
			location2.Y += height;
		}
		if (this.AfterDraw != null)
		{
			this.AfterDraw(this, new LayerEventArgs(this, mapViewport));
		}
	}
}
