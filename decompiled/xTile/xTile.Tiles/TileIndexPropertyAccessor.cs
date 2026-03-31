using System.Collections.Generic;
using xTile.ObjectModel;

namespace xTile.Tiles;

public class TileIndexPropertyAccessor
{
	private TileSheet m_tileSheet;

	private readonly Dictionary<int, TileIndexPropertyCollection> m_cache;

	public IPropertyCollection this[int tileIndex]
	{
		get
		{
			if (!m_cache.TryGetValue(tileIndex, out var value))
			{
				value = new TileIndexPropertyCollection(m_tileSheet, tileIndex);
				m_cache.Add(tileIndex, value);
			}
			return value;
		}
	}

	internal TileIndexPropertyAccessor(TileSheet tileSheet)
	{
		m_tileSheet = tileSheet;
		m_cache = new Dictionary<int, TileIndexPropertyCollection>();
	}
}
