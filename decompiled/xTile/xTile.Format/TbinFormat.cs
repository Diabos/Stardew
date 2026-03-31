using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace xTile.Format;

internal class TbinFormat : IMapFormat
{
	private const byte PROPERTY_BOOL = 0;

	private const byte PROPERTY_INT = 1;

	private const byte PROPERTY_FLOAT = 2;

	private const byte PROPERTY_STRING = 3;

	private CompatibilityReport m_compatibilityResults;

	private byte[] m_tbinSequence;

	private byte[] m_buffer = new byte[803];

	public string Name => "tIDE Binary Map File";

	public string FileExtensionDescriptor => "tIDE Binary Map Files (*.tbin)";

	public string FileExtension => "tbin";

	public TbinFormat()
	{
		m_compatibilityResults = new CompatibilityReport();
		m_tbinSequence = (from x in "tBIN10".ToArray()
			select (byte)x).ToArray();
	}

	public CompatibilityReport DetermineCompatibility(Map map)
	{
		return m_compatibilityResults;
	}

	public Map Load(Stream stream)
	{
		Map map = new Map();
		LoadSequence(stream, m_tbinSequence);
		map.Id = LoadString(stream);
		map.Description = LoadString(stream);
		LoadProperties(stream, map);
		LoadTileSheets(stream, map);
		LoadLayers(stream, map);
		return map;
	}

	public void Store(Map map, Stream stream)
	{
		StoreSequence(stream, m_tbinSequence);
		StoreString(stream, map.Id);
		StoreString(stream, map.Description);
		StoreProperties(stream, map);
		StoreTileSheets(stream, map);
		StoreLayers(stream, map);
	}

	private void StoreSequence(Stream stream, byte[] sequence)
	{
		stream.Write(sequence, 0, sequence.Length);
	}

	private byte[] GetBuffer(int len)
	{
		if (m_buffer.Length < len)
		{
			m_buffer = new byte[len];
		}
		return m_buffer;
	}

	private byte[] LoadSequence(Stream stream, int sequenceLength)
	{
		byte[] buffer = GetBuffer(sequenceLength);
		if (stream.Read(buffer, 0, sequenceLength) != sequenceLength)
		{
			throw new Exception("Unexpected end of file while reading sequence of length: " + sequenceLength);
		}
		return buffer;
	}

	private void LoadSequence(Stream stream, byte[] expectedSequence)
	{
		byte[] array = LoadSequence(stream, expectedSequence.Length);
		for (int i = 0; i < expectedSequence.Length; i++)
		{
			if (array[i] != expectedSequence[i])
			{
				throw new Exception(string.Concat("Byte sequence mismatch. Expected: ", expectedSequence, " Actual: ", array));
			}
		}
	}

	private void StoreBool(Stream stream, bool value)
	{
		stream.WriteByte(value ? ((byte)1) : ((byte)0));
	}

	private bool LoadBool(Stream stream)
	{
		return stream.ReadByte() > 0;
	}

	private void StoreInt32(Stream stream, int value)
	{
		stream.WriteByte((byte)(value & 0xFF));
		stream.WriteByte((byte)((value >> 8) & 0xFF));
		stream.WriteByte((byte)((value >> 16) & 0xFF));
		stream.WriteByte((byte)((value >> 24) & 0xFF));
	}

	private int LoadInt32(Stream stream)
	{
		byte[] array = LoadSequence(stream, 4);
		return (array[3] << 24) | (array[2] << 16) | (array[1] << 8) | array[0];
	}

	private void StoreUInt16(Stream stream, ushort value)
	{
		byte[] buffer = GetBuffer(2);
		buffer[0] = (byte)value;
		buffer[1] = (byte)(value >> 8);
		stream.Write(buffer, 0, 2);
	}

	private ushort LoadUInt16(Stream stream)
	{
		int num = stream.ReadByte();
		int num2 = stream.ReadByte();
		return (ushort)(num | (num2 << 8));
	}

	private void StoreFloat(Stream stream, float value)
	{
		byte[] bytes = BitConverter.GetBytes(value);
		StoreSequence(stream, bytes);
	}

	private float LoadFloat(Stream stream)
	{
		return BitConverter.ToSingle(LoadSequence(stream, 4), 0);
	}

	private void StoreString(Stream stream, string value)
	{
		byte[] bytes = new UTF8Encoding().GetBytes(value);
		StoreInt32(stream, (short)bytes.Length);
		stream.Write(bytes, 0, bytes.Length);
	}

	private string LoadString(Stream stream)
	{
		int num = LoadInt32(stream);
		byte[] bytes = LoadSequence(stream, num);
		return new UTF8Encoding().GetString(bytes, 0, num);
	}

	private void StoreSize(Stream stream, Size size)
	{
		StoreInt32(stream, size.Width);
		StoreInt32(stream, size.Height);
	}

	private Size LoadSize(Stream stream)
	{
		return new Size
		{
			Width = LoadInt32(stream),
			Height = LoadInt32(stream)
		};
	}

	private void StoreProperties(Stream stream, Component component)
	{
		StoreInt32(stream, component.Properties.Count);
		foreach (string key in component.Properties.Keys)
		{
			StoreString(stream, key);
			PropertyValue propertyValue = component.Properties[key];
			if (propertyValue.Type == typeof(bool))
			{
				stream.WriteByte(0);
				StoreBool(stream, propertyValue);
				continue;
			}
			if (propertyValue.Type == typeof(int))
			{
				stream.WriteByte(1);
				StoreInt32(stream, propertyValue);
				continue;
			}
			if (propertyValue.Type == typeof(float))
			{
				stream.WriteByte(2);
				StoreFloat(stream, propertyValue);
				continue;
			}
			if (!(propertyValue.Type == typeof(string)))
			{
				throw new Exception("Unsupported property type: " + propertyValue.Type);
			}
			stream.WriteByte(3);
			StoreString(stream, propertyValue);
		}
	}

	private void LoadProperties(Stream stream, Component component)
	{
		int num = LoadInt32(stream);
		while (num-- > 0)
		{
			string key = LoadString(stream);
			switch ((byte)stream.ReadByte())
			{
			case 0:
				component.Properties[key] = LoadBool(stream);
				break;
			case 1:
				component.Properties[key] = LoadInt32(stream);
				break;
			case 2:
				component.Properties[key] = LoadFloat(stream);
				break;
			case 3:
				component.Properties[key] = LoadString(stream);
				break;
			}
		}
	}

	private void StoreTileSheet(Stream stream, TileSheet tileSheet)
	{
		StoreString(stream, tileSheet.Id);
		StoreString(stream, tileSheet.Description);
		StoreString(stream, tileSheet.ImageSource);
		StoreSize(stream, tileSheet.SheetSize);
		StoreSize(stream, tileSheet.TileSize);
		StoreSize(stream, tileSheet.Margin);
		StoreSize(stream, tileSheet.Spacing);
		StoreProperties(stream, tileSheet);
	}

	private void LoadTileSheet(Stream stream, Map map)
	{
		string id = LoadString(stream);
		LoadString(stream);
		string imageSource = LoadString(stream);
		Size sheetSize = LoadSize(stream);
		Size tileSize = LoadSize(stream);
		Size margin = LoadSize(stream);
		Size spacing = LoadSize(stream);
		TileSheet tileSheet = new TileSheet(id, map, imageSource, sheetSize, tileSize);
		tileSheet.Margin = margin;
		tileSheet.Spacing = spacing;
		LoadProperties(stream, tileSheet);
		map.AddTileSheet(tileSheet);
	}

	private void StoreTileSheets(Stream stream, Map map)
	{
		StoreInt32(stream, map.TileSheets.Count);
		foreach (TileSheet tileSheet in map.TileSheets)
		{
			StoreTileSheet(stream, tileSheet);
		}
	}

	private void LoadTileSheets(Stream stream, Map map)
	{
		int num = LoadInt32(stream);
		while (num-- > 0)
		{
			LoadTileSheet(stream, map);
		}
	}

	private void StoreStaticTile(Stream stream, StaticTile staticTile)
	{
		StoreInt32(stream, staticTile.TileIndex);
		stream.WriteByte((byte)staticTile.BlendMode);
		StoreProperties(stream, staticTile);
	}

	private StaticTile LoadStaticTile(Stream stream, Layer layer, TileSheet tileSheet)
	{
		int tileIndex = LoadInt32(stream);
		BlendMode blendMode = (BlendMode)stream.ReadByte();
		StaticTile staticTile = new StaticTile(layer, tileSheet, blendMode, tileIndex);
		LoadProperties(stream, staticTile);
		return staticTile;
	}

	private void StoreAnimatedTile(Stream stream, AnimatedTile animatedTile)
	{
		StoreInt32(stream, (int)animatedTile.FrameInterval);
		StoreInt32(stream, animatedTile.TileFrames.Length);
		TileSheet tileSheet = null;
		StaticTile[] tileFrames = animatedTile.TileFrames;
		foreach (StaticTile staticTile in tileFrames)
		{
			TileSheet tileSheet2 = staticTile.TileSheet;
			if (tileSheet2 != tileSheet)
			{
				stream.WriteByte(84);
				StoreString(stream, tileSheet2.Id);
				tileSheet = tileSheet2;
			}
			stream.WriteByte(83);
			StoreStaticTile(stream, staticTile);
		}
		StoreProperties(stream, animatedTile);
	}

	private AnimatedTile LoadAnimatedTile(Stream stream, Layer layer)
	{
		long frameInterval = LoadInt32(stream);
		int num = LoadInt32(stream);
		List<StaticTile> list = new List<StaticTile>(num);
		Map map = layer.Map;
		TileSheet tileSheet = null;
		while (num > 0)
		{
			switch ((char)(ushort)stream.ReadByte())
			{
			case 'T':
			{
				string tileSheetId = LoadString(stream);
				tileSheet = map.GetTileSheet(tileSheetId);
				break;
			}
			default:
				throw new Exception("Expected character byte 'T' or 'S'");
			case 'S':
				list.Add(LoadStaticTile(stream, layer, tileSheet));
				num--;
				break;
			}
		}
		AnimatedTile animatedTile = new AnimatedTile(layer, list.ToArray(), frameInterval);
		LoadProperties(stream, animatedTile);
		return animatedTile;
	}

	private void StoreLayer(Stream stream, Layer layer)
	{
		StoreString(stream, layer.Id);
		StoreBool(stream, layer.Visible);
		StoreString(stream, layer.Description);
		StoreSize(stream, layer.LayerSize);
		StoreSize(stream, layer.TileSize);
		StoreProperties(stream, layer);
		TileSheet tileSheet = null;
		int num = 0;
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			for (int j = 0; j < layer.LayerWidth; j++)
			{
				Tile tile = layer.Tiles[j, i];
				if (tile == null)
				{
					num++;
					continue;
				}
				if (num > 0)
				{
					stream.WriteByte(78);
					StoreInt32(stream, num);
					num = 0;
				}
				TileSheet tileSheet2 = tile.TileSheet;
				if (tileSheet != tileSheet2)
				{
					stream.WriteByte(84);
					StoreString(stream, (tileSheet2 == null) ? "" : tileSheet2.Id);
					tileSheet = tileSheet2;
				}
				if (tile is StaticTile)
				{
					stream.WriteByte(83);
					StoreStaticTile(stream, (StaticTile)tile);
				}
				else if (tile is AnimatedTile)
				{
					stream.WriteByte(65);
					StoreAnimatedTile(stream, (AnimatedTile)tile);
				}
			}
			if (num > 0)
			{
				stream.WriteByte(78);
				StoreInt32(stream, num);
				num = 0;
			}
		}
	}

	private void LoadLayer(Stream stream, Map map)
	{
		string id = LoadString(stream);
		bool visible = LoadBool(stream);
		string description = LoadString(stream);
		Size layerSize = LoadSize(stream);
		Size tileSize = LoadSize(stream);
		tileSize.Width *= Layer.zoom;
		tileSize.Height *= Layer.zoom;
		Layer layer = new Layer(id, map, layerSize, tileSize);
		layer.Description = description;
		layer.Visible = visible;
		LoadProperties(stream, layer);
		Location origin = Location.Origin;
		TileSheet tileSheet = null;
		while (origin.Y < layerSize.Height)
		{
			origin.X = 0;
			while (origin.X < layerSize.Width)
			{
				switch ((char)(ushort)stream.ReadByte())
				{
				case 'S':
					layer.Tiles[origin] = LoadStaticTile(stream, layer, tileSheet);
					origin.X++;
					break;
				case 'T':
				{
					string tileSheetId = LoadString(stream);
					tileSheet = map.GetTileSheet(tileSheetId);
					break;
				}
				default:
					throw new Exception("Excpected character byte 'T', 'N', 'S' oe 'A'");
				case 'N':
				{
					int num = LoadInt32(stream);
					origin.X += num;
					break;
				}
				case 'A':
					layer.Tiles[origin] = LoadAnimatedTile(stream, layer);
					origin.X++;
					break;
				}
			}
			origin.Y++;
		}
		map.AddLayer(layer);
	}

	private void StoreLayers(Stream stream, Map map)
	{
		StoreInt32(stream, map.Layers.Count);
		foreach (Layer layer in map.Layers)
		{
			StoreLayer(stream, layer);
		}
	}

	private void LoadLayers(Stream stream, Map map)
	{
		int num = LoadInt32(stream);
		while (num-- > 0)
		{
			LoadLayer(stream, map);
		}
	}
}
