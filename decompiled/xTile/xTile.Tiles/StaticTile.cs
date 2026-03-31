using System;
using xTile.Layers;

namespace xTile.Tiles;

public class StaticTile : Tile
{
	private BlendMode m_blendMode;

	private TileSheet m_tileSheet;

	private int m_tileIndex;

	public override BlendMode BlendMode
	{
		get
		{
			return m_blendMode;
		}
		set
		{
			m_blendMode = value;
		}
	}

	public override TileSheet TileSheet => m_tileSheet;

	public override int TileIndex
	{
		get
		{
			return m_tileIndex;
		}
		set
		{
			m_tileIndex = value;
		}
	}

	public StaticTile(Layer layer, TileSheet tileSheet, BlendMode blendMode, int tileIndex)
		: base(layer)
	{
		if (!layer.Map.TileSheets.Contains(tileSheet))
		{
			throw new Exception("The specified TileSheet is not in the parent map");
		}
		m_blendMode = blendMode;
		m_tileSheet = tileSheet;
		if (tileIndex >= 0)
		{
			_ = tileSheet.TileCount;
		}
		m_tileIndex = tileIndex;
	}

	public override bool DependsOnTileSheet(TileSheet tileSheet)
	{
		return m_tileSheet == tileSheet;
	}

	public override Tile Clone(Layer layer)
	{
		return new StaticTile(layer, TileSheet, BlendMode, m_tileIndex);
	}

	public override string ToString()
	{
		return "Static Tile Index=" + m_tileIndex + " BlendMode=" + BlendMode;
	}
}
