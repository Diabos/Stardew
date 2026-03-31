using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace xTile.Format;

internal class TideFormat : IMapFormat
{
	private CompatibilityReport m_compatibilityResults;

	public string Name => "tIDE Map File";

	public string FileExtensionDescriptor => "tIDE Map Files (*.tide)";

	public string FileExtension => "tide";

	public CompatibilityReport DetermineCompatibility(Map map)
	{
		return m_compatibilityResults;
	}

	public Map Load(Stream stream)
	{
		XmlHelper xmlHelper = new XmlHelper(new XmlTextReader(stream)
		{
			WhitespaceHandling = WhitespaceHandling.None
		});
		xmlHelper.AdvanceDeclaration();
		xmlHelper.AdvanceStartElement("Map");
		Map map = new Map(xmlHelper.GetAttribute("Id"));
		xmlHelper.AdvanceStartElement("Description");
		string cData = xmlHelper.GetCData();
		xmlHelper.AdvanceEndElement("Description");
		map.Description = cData;
		LoadTileSheets(xmlHelper, map);
		LoadLayers(xmlHelper, map);
		LoadProperties(xmlHelper, map);
		return map;
	}

	public void Store(Map map, Stream stream)
	{
		XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, Encoding.UTF8);
		xmlTextWriter.Formatting = Formatting.Indented;
		xmlTextWriter.WriteStartDocument();
		xmlTextWriter.WriteStartElement("Map");
		xmlTextWriter.WriteAttributeString("Id", map.Id);
		xmlTextWriter.WriteStartElement("Description");
		xmlTextWriter.WriteCData(map.Description);
		xmlTextWriter.WriteEndElement();
		StoreTileSheets(map.TileSheets, xmlTextWriter);
		StoreLayers(map.Layers, xmlTextWriter);
		StoreProperties(map, xmlTextWriter);
		xmlTextWriter.WriteEndElement();
		xmlTextWriter.Flush();
	}

	internal TideFormat()
	{
		m_compatibilityResults = new CompatibilityReport();
	}

	private void LoadProperties(XmlHelper xmlHelper, Component component)
	{
		if (xmlHelper.XmlReader.Name != "Properties")
		{
			xmlHelper.AdvanceStartElement("Properties");
		}
		while (xmlHelper.AdvanceStartRepeatedElement("Property", "Properties"))
		{
			string attribute = xmlHelper.GetAttribute("Key");
			string attribute2 = xmlHelper.GetAttribute("Type");
			string cData = xmlHelper.GetCData();
			if (attribute2 == typeof(bool).Name)
			{
				component.Properties[attribute] = bool.Parse(cData);
			}
			else if (attribute2 == typeof(int).Name)
			{
				component.Properties[attribute] = int.Parse(cData);
			}
			else if (attribute2 == typeof(float).Name)
			{
				component.Properties[attribute] = float.Parse(cData);
			}
			else
			{
				component.Properties[attribute] = cData;
			}
			xmlHelper.AdvanceEndElement("Property");
		}
	}

	private void StoreProperties(Component component, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("Properties");
		foreach (KeyValuePair<string, PropertyValue> property in component.Properties)
		{
			xmlWriter.WriteStartElement("Property");
			xmlWriter.WriteAttributeString("Key", property.Key);
			xmlWriter.WriteAttributeString("Type", property.Value.Type.Name);
			xmlWriter.WriteCData(property.Value);
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
	}

	private StaticTile LoadStaticTile(XmlHelper xmlHelper, Layer layer, TileSheet tileSheet)
	{
		int intAttribute = xmlHelper.GetIntAttribute("Index");
		BlendMode blendMode = ((!(xmlHelper.GetAttribute("BlendMode") == BlendMode.Alpha.ToString())) ? BlendMode.Additive : BlendMode.Alpha);
		StaticTile staticTile = new StaticTile(layer, tileSheet, blendMode, intAttribute);
		if (!xmlHelper.XmlReader.IsEmptyElement)
		{
			LoadProperties(xmlHelper, staticTile);
			xmlHelper.AdvanceEndElement("Static");
		}
		return staticTile;
	}

	private void StoreStaticTile(StaticTile staticTile, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("Static");
		xmlWriter.WriteAttributeString("Index", staticTile.TileIndex.ToString());
		xmlWriter.WriteAttributeString("BlendMode", staticTile.BlendMode.ToString());
		if (staticTile.Properties.Count > 0)
		{
			StoreProperties(staticTile, xmlWriter);
		}
		xmlWriter.WriteEndElement();
	}

	private AnimatedTile LoadAnimatedTile(XmlHelper xmlHelper, Layer layer, TileSheet tileSheet)
	{
		int intAttribute = xmlHelper.GetIntAttribute("Interval");
		xmlHelper.AdvanceStartElement("Frames");
		Map map = layer.Map;
		List<StaticTile> list = new List<StaticTile>();
		while (xmlHelper.AdvanceNode() != XmlNodeType.EndElement)
		{
			if (xmlHelper.XmlReader.Name == "Static")
			{
				list.Add(LoadStaticTile(xmlHelper, layer, tileSheet));
			}
			else if (xmlHelper.XmlReader.Name == "TileSheet")
			{
				string attribute = xmlHelper.GetAttribute("Ref");
				tileSheet = map.GetTileSheet(attribute);
			}
		}
		AnimatedTile animatedTile = new AnimatedTile(layer, list.ToArray(), intAttribute);
		if (xmlHelper.AdvanceNode() != XmlNodeType.EndElement)
		{
			LoadProperties(xmlHelper, animatedTile);
			xmlHelper.AdvanceNode();
		}
		return animatedTile;
	}

	private void StoreAnimatedTile(AnimatedTile animatedTile, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("Animated");
		xmlWriter.WriteAttributeString("Interval", animatedTile.FrameInterval.ToString());
		xmlWriter.WriteStartElement("Frames");
		TileSheet tileSheet = null;
		StaticTile[] tileFrames = animatedTile.TileFrames;
		foreach (StaticTile staticTile in tileFrames)
		{
			if (tileSheet != staticTile.TileSheet)
			{
				xmlWriter.WriteStartElement("TileSheet");
				xmlWriter.WriteAttributeString("Ref", staticTile.TileSheet.Id);
				xmlWriter.WriteEndElement();
				tileSheet = staticTile.TileSheet;
			}
			StoreStaticTile(staticTile, xmlWriter);
		}
		xmlWriter.WriteEndElement();
		if (animatedTile.Properties.Count > 0)
		{
			StoreProperties(animatedTile, xmlWriter);
		}
		xmlWriter.WriteEndElement();
	}

	private void LoadTileSheet(XmlHelper xmlHelper, Map map)
	{
		string attribute = xmlHelper.GetAttribute("Id");
		xmlHelper.AdvanceStartElement("Description");
		xmlHelper.GetCData();
		xmlHelper.AdvanceEndElement("Description");
		xmlHelper.AdvanceStartElement("ImageSource");
		string cData = xmlHelper.GetCData();
		xmlHelper.AdvanceEndElement("ImageSource");
		xmlHelper.AdvanceStartElement("Alignment");
		Size sheetSize = Size.FromString(xmlHelper.GetAttribute("SheetSize"));
		Size tileSize = Size.FromString(xmlHelper.GetAttribute("TileSize"));
		Size margin = Size.FromString(xmlHelper.GetAttribute("Margin"));
		Size spacing = Size.FromString(xmlHelper.GetAttribute("Spacing"));
		xmlHelper.AdvanceEndElement("Alignment");
		TileSheet tileSheet = new TileSheet(attribute, map, cData, sheetSize, tileSize);
		tileSheet.Margin = margin;
		tileSheet.Spacing = spacing;
		LoadProperties(xmlHelper, tileSheet);
		xmlHelper.AdvanceEndElement("TileSheet");
		map.AddTileSheet(tileSheet);
	}

	private void StoreTileSheet(TileSheet tileSheet, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("TileSheet");
		xmlWriter.WriteAttributeString("Id", tileSheet.Id);
		xmlWriter.WriteStartElement("Description");
		xmlWriter.WriteCData(tileSheet.Description);
		xmlWriter.WriteEndElement();
		xmlWriter.WriteStartElement("ImageSource");
		xmlWriter.WriteCData(tileSheet.ImageSource);
		xmlWriter.WriteEndElement();
		xmlWriter.WriteStartElement("Alignment");
		xmlWriter.WriteAttributeString("SheetSize", tileSheet.SheetSize.ToString());
		xmlWriter.WriteAttributeString("TileSize", tileSheet.TileSize.ToString());
		xmlWriter.WriteAttributeString("Margin", tileSheet.Margin.ToString());
		xmlWriter.WriteAttributeString("Spacing", tileSheet.Spacing.ToString());
		xmlWriter.WriteEndElement();
		StoreProperties(tileSheet, xmlWriter);
		xmlWriter.WriteEndElement();
	}

	private void LoadTileSheets(XmlHelper xmlHelper, Map map)
	{
		xmlHelper.AdvanceStartElement("TileSheets");
		while (xmlHelper.AdvanceStartRepeatedElement("TileSheet", "TileSheets"))
		{
			LoadTileSheet(xmlHelper, map);
		}
	}

	private void StoreTileSheets(ReadOnlyCollection<TileSheet> tileSheets, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("TileSheets");
		foreach (TileSheet tileSheet in tileSheets)
		{
			StoreTileSheet(tileSheet, xmlWriter);
		}
		xmlWriter.WriteEndElement();
	}

	private void LoadLayer(XmlHelper xmlHelper, Map map)
	{
		string attribute = xmlHelper.GetAttribute("Id");
		bool visible = bool.Parse(xmlHelper.GetAttribute("Visible"));
		xmlHelper.AdvanceStartElement("Description");
		string cData = xmlHelper.GetCData();
		xmlHelper.AdvanceEndElement("Description");
		xmlHelper.AdvanceStartElement("Dimensions");
		Size layerSize = Size.FromString(xmlHelper.GetAttribute("LayerSize"));
		Size tileSize = Size.FromString(xmlHelper.GetAttribute("TileSize"));
		xmlHelper.AdvanceEndElement("Dimensions");
		Layer layer = new Layer(attribute, map, layerSize, tileSize);
		layer.Description = cData;
		layer.Visible = visible;
		xmlHelper.AdvanceStartElement("TileArray");
		Location origin = Location.Origin;
		TileSheet tileSheet = null;
		XmlReader xmlReader = xmlHelper.XmlReader;
		while (xmlHelper.AdvanceStartRepeatedElement("Row", "TileArray"))
		{
			origin.X = 0;
			while (xmlHelper.AdvanceNode() != XmlNodeType.EndElement)
			{
				if (xmlReader.Name == "Null")
				{
					int intAttribute = xmlHelper.GetIntAttribute("Count");
					origin.X += intAttribute % layerSize.Width;
				}
				else if (xmlReader.Name == "TileSheet")
				{
					string attribute2 = xmlHelper.GetAttribute("Ref");
					tileSheet = map.GetTileSheet(attribute2);
				}
				else if (xmlReader.Name == "Static")
				{
					layer.Tiles[origin] = LoadStaticTile(xmlHelper, layer, tileSheet);
					origin.X++;
				}
				else if (xmlReader.Name == "Animated")
				{
					layer.Tiles[origin] = LoadAnimatedTile(xmlHelper, layer, tileSheet);
					origin.X++;
				}
			}
			origin.Y++;
		}
		LoadProperties(xmlHelper, layer);
		xmlHelper.AdvanceEndElement("Layer");
		map.AddLayer(layer);
	}

	private void StoreLayer(Layer layer, XmlWriter xmlWriter)
	{
		xmlWriter.WriteStartElement("Layer");
		xmlWriter.WriteAttributeString("Id", layer.Id);
		xmlWriter.WriteAttributeString("Visible", layer.Visible.ToString());
		xmlWriter.WriteStartElement("Description");
		xmlWriter.WriteCData(layer.Description);
		xmlWriter.WriteEndElement();
		xmlWriter.WriteStartElement("Dimensions");
		xmlWriter.WriteAttributeString("LayerSize", layer.LayerSize.ToString());
		xmlWriter.WriteAttributeString("TileSize", layer.TileSize.ToString());
		xmlWriter.WriteEndElement();
		xmlWriter.WriteStartElement("TileArray");
		TileSheet tileSheet = null;
		int num = 0;
		for (int i = 0; i < layer.LayerHeight; i++)
		{
			xmlWriter.WriteStartElement("Row");
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
					xmlWriter.WriteStartElement("Null");
					xmlWriter.WriteAttributeString("Count", num.ToString());
					xmlWriter.WriteEndElement();
					num = 0;
				}
				TileSheet tileSheet2 = tile.TileSheet;
				if (tileSheet != tileSheet2)
				{
					xmlWriter.WriteStartElement("TileSheet");
					xmlWriter.WriteAttributeString("Ref", (tileSheet2 == null) ? "" : tileSheet2.Id);
					xmlWriter.WriteEndElement();
					tileSheet = tileSheet2;
				}
				if (tile is StaticTile)
				{
					StoreStaticTile((StaticTile)tile, xmlWriter);
				}
				else if (tile is AnimatedTile)
				{
					AnimatedTile animatedTile = (AnimatedTile)tile;
					StoreAnimatedTile(animatedTile, xmlWriter);
				}
			}
			if (num > 0)
			{
				xmlWriter.WriteStartElement("Null");
				xmlWriter.WriteAttributeString("Count", num.ToString());
				xmlWriter.WriteEndElement();
				num = 0;
			}
			xmlWriter.WriteEndElement();
		}
		xmlWriter.WriteEndElement();
		StoreProperties(layer, xmlWriter);
		xmlWriter.WriteEndElement();
	}

	private void LoadLayers(XmlHelper xmlHelper, Map map)
	{
		xmlHelper.AdvanceStartElement("Layers");
		while (xmlHelper.AdvanceStartRepeatedElement("Layer", "Layers"))
		{
			LoadLayer(xmlHelper, map);
		}
	}

	private void StoreLayers(ReadOnlyCollection<Layer> layers, XmlWriter xmlTextWriter)
	{
		xmlTextWriter.WriteStartElement("Layers");
		foreach (Layer layer in layers)
		{
			StoreLayer(layer, xmlTextWriter);
		}
		xmlTextWriter.WriteEndElement();
	}
}
