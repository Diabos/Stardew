using System.IO;
using Microsoft.Xna.Framework.Content;
using xTile.Format;

namespace xTile.Pipeline;

public class TideReader : ContentTypeReader<Map>
{
	protected override Map Read(ContentReader contentReader, Map existingMap)
	{
		int count = contentReader.ReadInt32();
		MemoryStream stream = new MemoryStream(contentReader.ReadBytes(count));
		Map map = FormatManager.Instance.BinaryFormat.Load(stream);
		if (map != null)
		{
			map.assetPath = contentReader.AssetName;
			map.FlattenTileSheetPaths();
		}
		return map;
	}
}
