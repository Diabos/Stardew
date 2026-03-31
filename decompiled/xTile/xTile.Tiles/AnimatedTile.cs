using System;
using System.Collections.Generic;
using System.Text;
using xTile.Layers;

namespace xTile.Tiles;

public class AnimatedTile : Tile
{
	private StaticTile[] m_tileFrames;

	private long m_frameInterval;

	private long m_animationInterval;

	public override BlendMode BlendMode
	{
		get
		{
			int num = (int)(base.Layer.Map.ElapsedTime % m_animationInterval / m_frameInterval);
			return m_tileFrames[num].BlendMode;
		}
		set
		{
			StaticTile[] tileFrames = m_tileFrames;
			for (int i = 0; i < tileFrames.Length; i++)
			{
				tileFrames[i].BlendMode = value;
			}
		}
	}

	public override TileSheet TileSheet
	{
		get
		{
			int num = (int)(base.Layer.Map.ElapsedTime % m_animationInterval / m_frameInterval);
			return m_tileFrames[num].TileSheet;
		}
	}

	public override int TileIndex
	{
		get
		{
			int num = (int)(base.Layer.Map.ElapsedTime % m_animationInterval / m_frameInterval);
			return m_tileFrames[num].TileIndex;
		}
		set
		{
		}
	}

	public StaticTile[] TileFrames
	{
		get
		{
			StaticTile[] array = new StaticTile[m_tileFrames.Length];
			m_tileFrames.CopyTo(array, 0);
			return array;
		}
	}

	public long FrameInterval => m_frameInterval;

	public AnimatedTile(Layer layer, StaticTile[] tileFrames, long frameInterval)
		: base(layer)
	{
		if (frameInterval <= 0)
		{
			throw new Exception("Frame interval must be strictly positive");
		}
		m_tileFrames = new StaticTile[tileFrames.Length];
		tileFrames.CopyTo(m_tileFrames, 0);
		m_frameInterval = frameInterval;
		m_animationInterval = frameInterval * tileFrames.Length;
	}

	public override bool DependsOnTileSheet(TileSheet tileSheet)
	{
		StaticTile[] tileFrames = m_tileFrames;
		for (int i = 0; i < tileFrames.Length; i++)
		{
			if (tileFrames[i].DependsOnTileSheet(tileSheet))
			{
				return true;
			}
		}
		return false;
	}

	public override Tile Clone(Layer layer)
	{
		List<StaticTile> list = new List<StaticTile>(m_tileFrames.Length);
		StaticTile[] tileFrames = m_tileFrames;
		foreach (StaticTile staticTile in tileFrames)
		{
			list.Add((StaticTile)staticTile.Clone(layer));
		}
		return new AnimatedTile(layer, list.ToArray(), m_frameInterval);
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Animated Frames=");
		StaticTile[] tileFrames = m_tileFrames;
		foreach (StaticTile staticTile in tileFrames)
		{
			stringBuilder.Append(staticTile.ToString());
			stringBuilder.Append(' ');
		}
		stringBuilder.Append(" Interval=");
		stringBuilder.Append(m_frameInterval);
		stringBuilder.Append(" BlendMode=");
		stringBuilder.Append(BlendMode);
		return stringBuilder.ToString();
	}
}
