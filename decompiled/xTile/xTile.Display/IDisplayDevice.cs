using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Tiles;

namespace xTile.Display;

public interface IDisplayDevice
{
	void LoadTileSheet(TileSheet tileSheet);

	void DisposeTileSheet(TileSheet tileSheet);

	void BeginScene(SpriteBatch b);

	void SetClippingRegion(Rectangle clippingRegion);

	void DrawTile(Tile tile, Location location, float layerDepth);

	void EndScene();
}
