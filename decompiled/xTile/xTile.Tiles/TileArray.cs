using System;
using System.Collections.ObjectModel;
using xTile.Dimensions;
using xTile.Layers;

namespace xTile.Tiles;

public class TileArray
{
	private Layer m_layer;

	private ReadOnlyCollection<TileSheet> m_tileSheets;

	private Tile[,] m_tiles;

	public Tile[,] Array => m_tiles;

	public Tile this[int x, int y]
	{
		get
		{
			if (x < 0 || x >= m_layer.LayerSize.Width || y < 0 || y >= m_layer.LayerSize.Height)
			{
				return null;
			}
			return m_tiles[x, y];
		}
		set
		{
			if (value == null)
			{
				m_tiles[x, y] = null;
				return;
			}
			if (!m_tileSheets.Contains(value.TileSheet))
			{
				throw new Exception("The tile contains an invalid TileSheet reference");
			}
			if (value.TileIndex >= 0)
			{
				_ = value.TileIndex;
				_ = value.TileSheet.TileCount;
			}
			m_tiles[x, y] = value;
			m_layer.MarkRowDirty(y);
		}
	}

	public Tile this[Location location]
	{
		get
		{
			return this[location.X, location.Y];
		}
		set
		{
			this[location.X, location.Y] = value;
			m_layer.MarkRowDirty(location.Y);
		}
	}

	public TileArray(Layer layer, Tile[,] tiles)
	{
		m_layer = layer;
		m_tileSheets = m_layer.Map.TileSheets;
		m_tiles = tiles;
	}
}
