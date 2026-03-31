using xTile.Layers;
using xTile.ObjectModel;

namespace xTile.Tiles;

public abstract class Tile : Component
{
	private Layer m_layer;

	public Layer Layer => m_layer;

	public abstract BlendMode BlendMode { get; set; }

	public abstract TileSheet TileSheet { get; }

	public abstract int TileIndex { get; set; }

	public IPropertyCollection TileIndexProperties => TileSheet.TileIndexProperties[TileIndex];

	public Tile(Layer layer)
		: base(string.Empty)
	{
		m_layer = layer;
	}

	public abstract bool DependsOnTileSheet(TileSheet tileSheet);

	public abstract Tile Clone(Layer layer);
}
