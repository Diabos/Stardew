using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace xTile.Format;

public class FormatManager
{
	private static FormatManager s_formatManager = new FormatManager();

	private Dictionary<string, IMapFormat> m_mapFormats;

	private TideFormat m_defaultFormat;

	private TbinFormat m_binaryFormat;

	public static FormatManager Instance => s_formatManager;

	public IMapFormat this[string mapFormatName]
	{
		get
		{
			if (!m_mapFormats.ContainsKey(mapFormatName))
			{
				throw new Exception("Map format '" + mapFormatName + "' is is not registered");
			}
			return m_mapFormats[mapFormatName];
		}
	}

	public IMapFormat DefaultFormat => m_defaultFormat;

	public IMapFormat BinaryFormat => m_binaryFormat;

	public ReadOnlyCollection<IMapFormat> MapFormats => new List<IMapFormat>(m_mapFormats.Values).AsReadOnly();

	public void RegisterMapFormat(IMapFormat mapFormat)
	{
		if (m_mapFormats.ContainsKey(mapFormat.Name))
		{
			throw new Exception("Map format '" + mapFormat.Name + "' is already registered");
		}
		m_mapFormats[mapFormat.Name] = mapFormat;
	}

	public void UnregisterMapFormat(IMapFormat mapFormat)
	{
		if (!m_mapFormats.ContainsKey(mapFormat.Name))
		{
			throw new Exception("Map format '" + mapFormat.Name + "' is is not registered");
		}
		m_mapFormats.Remove(mapFormat.Name);
	}

	public IMapFormat GetMapFormatByExtension(string fileExtension)
	{
		foreach (IMapFormat value in m_mapFormats.Values)
		{
			if (value.FileExtension.Equals(fileExtension, StringComparison.InvariantCultureIgnoreCase))
			{
				return value;
			}
		}
		return null;
	}

	public Map LoadMap(string filePath)
	{
		try
		{
			if (filePath == null)
			{
				throw new Exception("A null file path was specified");
			}
			string text = Path.GetExtension(filePath).Replace(".", "");
			if (text.Length == 0)
			{
				throw new Exception("Cannot determine map format without a file extension");
			}
			IMapFormat obj = GetMapFormatByExtension(text) ?? throw new Exception("No IMapFormat implementation for files with extension '" + text + "'");
			Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			Map result = obj.Load(stream);
			stream.Close();
			return result;
		}
		catch (Exception innerException)
		{
			throw new Exception("Unable to load map with file path '" + filePath + "'", innerException);
		}
	}

	private FormatManager()
	{
		m_mapFormats = new Dictionary<string, IMapFormat>();
		m_defaultFormat = new TideFormat();
		m_mapFormats[m_defaultFormat.Name] = m_defaultFormat;
		m_binaryFormat = new TbinFormat();
		m_mapFormats[m_binaryFormat.Name] = m_binaryFormat;
	}
}
