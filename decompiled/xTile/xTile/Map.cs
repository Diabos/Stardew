using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using xTile.Dimensions;
using xTile.Display;
using xTile.Layers;
using xTile.ObjectModel;
using xTile.Tiles;

namespace xTile;

public class Map : DescribedComponent
{
	private readonly List<TileSheet> m_tileSheets;

	private readonly ReadOnlyCollection<TileSheet> m_readOnlyTileSheets;

	private readonly List<Layer> m_layers;

	private readonly Dictionary<string, Layer> m_layersById;

	private readonly ReadOnlyCollection<Layer> m_readOnlyLayers;

	private long m_elapsedTime;

	private Size m_displaySize;

	public string assetPath;

	public Size DisplaySize => m_displaySize;

	public int DisplayWidth => m_displaySize.Width;

	public int DisplayHeight => m_displaySize.Height;

	public ReadOnlyCollection<Layer> Layers => m_readOnlyLayers;

	public ReadOnlyCollection<TileSheet> TileSheets => m_readOnlyTileSheets;

	public long ElapsedTime
	{
		get
		{
			return m_elapsedTime;
		}
		set
		{
			m_elapsedTime = value;
		}
	}

	public Map()
		: base("Untiled map")
	{
		m_tileSheets = new List<TileSheet>();
		m_readOnlyTileSheets = new ReadOnlyCollection<TileSheet>(m_tileSheets);
		m_layers = new List<Layer>();
		m_readOnlyLayers = new ReadOnlyCollection<Layer>(m_layers);
		m_layersById = new Dictionary<string, Layer>();
		m_elapsedTime = 0L;
	}

	public Map(string id)
		: base(id)
	{
		m_tileSheets = new List<TileSheet>();
		m_readOnlyTileSheets = new ReadOnlyCollection<TileSheet>(m_tileSheets);
		m_layers = new List<Layer>();
		m_readOnlyLayers = new ReadOnlyCollection<Layer>(m_layers);
		m_layersById = new Dictionary<string, Layer>();
		m_elapsedTime = 0L;
		m_displaySize = Size.Zero;
	}

	public string GetFullAssetPath(string relative_path)
	{
		if (assetPath == null)
		{
			return relative_path;
		}
		return Path.Combine(Path.GetDirectoryName(assetPath.Replace('\\', Path.DirectorySeparatorChar)), relative_path.Replace('\\', Path.DirectorySeparatorChar));
	}

	public void FlattenTileSheetPaths()
	{
		foreach (TileSheet tileSheet in m_tileSheets)
		{
			tileSheet.ImageSource.Replace('\\', Path.DirectorySeparatorChar);
			tileSheet.ImageSource = GetFullAssetPath(tileSheet.ImageSource);
		}
	}

	public Layer GetLayer(string layerId)
	{
		m_layersById.TryGetValue(layerId, out var value);
		return value;
	}

	public void AddLayer(Layer layer)
	{
		InsertLayer(layer, m_layers.Count);
	}

	public void InsertLayer(Layer layer, int layerIndex)
	{
		if (layer.Map != this)
		{
			throw new Exception("The specified Layer was not created for use with this map");
		}
		if (m_layers.Contains(layer))
		{
			throw new Exception("The specified Layer is already associated with this map");
		}
		if (layerIndex < 0 || layerIndex > m_layers.Count)
		{
			throw new Exception("The specified layer index is out of range");
		}
		m_layers.Insert(layerIndex, layer);
		m_layersById.Add(layer.Id, layer);
		UpdateDisplaySize();
	}

	public void RemoveLayer(Layer layer)
	{
		if (!m_layers.Contains(layer))
		{
			throw new Exception("The specified Layer is not contained in this map");
		}
		m_layers.Remove(layer);
		m_layersById.Remove(layer.Id);
		UpdateDisplaySize();
	}

	public void BringLayerForward(Layer layer)
	{
		int num = m_layers.IndexOf(layer);
		if (num < 0)
		{
			throw new Exception("The specified Layer is not contained in this map");
		}
		if (num != m_layers.Count - 1)
		{
			m_layers[num] = m_layers[num + 1];
			m_layers[num + 1] = layer;
		}
	}

	public void SendLayerBackward(Layer layer)
	{
		int num = m_layers.IndexOf(layer);
		if (num < 0)
		{
			throw new Exception("The specified Layer is not contained in this map");
		}
		if (num != 0)
		{
			m_layers[num] = m_layers[num - 1];
			m_layers[num - 1] = layer;
		}
	}

	public bool DependsOnTileSheet(TileSheet tileSheet)
	{
		if (tileSheet.Map != this)
		{
			return false;
		}
		foreach (Layer layer in m_layers)
		{
			if (layer.DependsOnTileSheet(tileSheet))
			{
				return true;
			}
		}
		return false;
	}

	public TileSheet GetTileSheet(string tileSheetId)
	{
		foreach (TileSheet tileSheet in m_tileSheets)
		{
			if (tileSheet.Id == tileSheetId)
			{
				return tileSheet;
			}
		}
		return null;
	}

	public void AddTileSheet(TileSheet tileSheet)
	{
		if (tileSheet.Map != this)
		{
			throw new Exception("The specified TileSheet was not created for use with this map");
		}
		if (m_tileSheets.Contains(tileSheet))
		{
			throw new Exception("The specified TileSheet is already associated with this map");
		}
		m_tileSheets.Add(tileSheet);
		m_tileSheets.Sort((TileSheet tileSheet2, TileSheet tileSheet3) => string.Compare(tileSheet2.Id, tileSheet3.Id, StringComparison.OrdinalIgnoreCase));
	}

	public void RemoveTileSheet(TileSheet tileSheet)
	{
		if (!m_tileSheets.Contains(tileSheet))
		{
			throw new Exception("The specified TileSheet is not contained in this map");
		}
		foreach (Layer layer in m_layers)
		{
			if (layer.DependsOnTileSheet(tileSheet))
			{
				throw new Exception("Cannot remove TileSheet as it is in use by Layer " + layer.Id);
			}
		}
		m_tileSheets.Remove(tileSheet);
	}

	public void RemoveTileSheetDependencies(TileSheet tileSheet)
	{
		foreach (Layer layer in m_layers)
		{
			layer.RemoveTileSheetDependency(tileSheet);
		}
	}

	public void Update(long timeInterval)
	{
		m_elapsedTime += timeInterval;
	}

	public void LoadTileSheets(IDisplayDevice displayDevice)
	{
		foreach (TileSheet tileSheet in m_tileSheets)
		{
			displayDevice.LoadTileSheet(tileSheet);
		}
	}

	public void DisposeTileSheets(IDisplayDevice displayDevice)
	{
		foreach (TileSheet tileSheet in m_tileSheets)
		{
			displayDevice.DisposeTileSheet(tileSheet);
		}
	}

	public void Draw(IDisplayDevice displayDevice, Rectangle mapViewport)
	{
		Draw(displayDevice, mapViewport, Location.Origin, wrapAround: false);
	}

	public void Draw(IDisplayDevice displayDevice, Rectangle mapViewport, Location displayOffset, bool wrapAround)
	{
	}

	internal void UpdateDisplaySize()
	{
		m_displaySize = Size.Zero;
		foreach (Layer layer in m_layers)
		{
			Size displaySize = layer.DisplaySize;
			m_displaySize.Width = Math.Max(m_displaySize.Width, displaySize.Width);
			m_displaySize.Height = Math.Max(m_displaySize.Height, displaySize.Height);
		}
	}
}
