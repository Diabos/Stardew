using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using xTile.ObjectModel;

namespace xTile.Tiles;

internal class TileIndexPropertyCollection : IPropertyCollection, IDictionary<string, PropertyValue>, ICollection<KeyValuePair<string, PropertyValue>>, IEnumerable<KeyValuePair<string, PropertyValue>>, IEnumerable
{
	private readonly Dictionary<string, string> m_indexKeys;

	private readonly TileSheet m_tileSheet;

	private readonly int m_tileIndex;

	private readonly string m_tileIndexString;

	private readonly string m_tileIndexKey;

	private static readonly char[] At = new char[1] { '@' };

	public ICollection<string> Keys => new List<string>(from x in m_tileSheet.Properties.Keys
		select ParseIndexedKey(x) into x
		where x != null
		select x);

	public ICollection<PropertyValue> Values
	{
		get
		{
			IPropertyCollection properties = m_tileSheet.Properties;
			List<PropertyValue> list = new List<PropertyValue>();
			foreach (KeyValuePair<string, PropertyValue> item in properties)
			{
				if (ParseIndexedKey(item.Key) != null)
				{
					list.Add(item.Value);
				}
			}
			return list;
		}
	}

	public PropertyValue this[string key]
	{
		get
		{
			return m_tileSheet.Properties[IndexKey(key)];
		}
		set
		{
			m_tileSheet.Properties[IndexKey(key)] = value;
		}
	}

	public int Count => Keys.Count;

	public bool IsReadOnly => m_tileSheet.Properties.IsReadOnly;

	internal TileIndexPropertyCollection(TileSheet tileSheet, int tileIndex)
	{
		m_tileSheet = tileSheet;
		m_tileIndex = tileIndex;
		m_tileIndexString = m_tileIndex.ToString();
		m_tileIndexKey = "@TileIndex@" + m_tileIndexString + "@";
		m_indexKeys = new Dictionary<string, string>(12);
	}

	private string IndexKey(string key)
	{
		if (!m_indexKeys.TryGetValue(key, out var value))
		{
			value = m_tileIndexKey + key;
			m_indexKeys.Add(key, value);
		}
		return value;
	}

	private string ParseIndexedKey(string indexedKey)
	{
		string[] array = indexedKey.Split(At);
		if (array.Length != 4)
		{
			return null;
		}
		if (array[0].Length != 0)
		{
			return null;
		}
		if (!array[1].Equals("TileIndex", StringComparison.Ordinal))
		{
			return null;
		}
		if (!array[2].Equals(m_tileIndexString, StringComparison.Ordinal))
		{
			return null;
		}
		return array[3];
	}

	public void CopyFrom(IPropertyCollection propertyCollection)
	{
		Clear();
		IPropertyCollection properties = m_tileSheet.Properties;
		foreach (string key in propertyCollection.Keys)
		{
			properties[IndexKey(key)] = propertyCollection[key];
		}
	}

	public void Add(string key, PropertyValue propertyValue)
	{
		m_tileSheet.Properties[IndexKey(key)] = propertyValue;
	}

	public bool ContainsKey(string key)
	{
		return m_tileSheet.Properties.ContainsKey(IndexKey(key));
	}

	public bool Remove(string key)
	{
		return m_tileSheet.Properties.Remove(IndexKey(key));
	}

	public bool TryGetValue(string key, out PropertyValue propertyValue)
	{
		return m_tileSheet.Properties.TryGetValue(IndexKey(key), out propertyValue);
	}

	public void Add(KeyValuePair<string, PropertyValue> keyValuPair)
	{
		this[keyValuPair.Key] = keyValuPair.Value;
	}

	public void Clear()
	{
		IPropertyCollection properties = m_tileSheet.Properties;
		foreach (string item in (IEnumerable<string>)properties.Keys.Where((string x) => ParseIndexedKey(x) != null).ToArray())
		{
			properties.Remove(item);
		}
	}

	public bool Contains(KeyValuePair<string, PropertyValue> keyValuePair)
	{
		return m_tileSheet.Properties.Contains(new KeyValuePair<string, PropertyValue>(IndexKey(keyValuePair.Key), keyValuePair.Value));
	}

	public void CopyTo(KeyValuePair<string, PropertyValue>[] array, int arrayIndex)
	{
		foreach (string key in Keys)
		{
			PropertyValue value = this[key];
			array[arrayIndex++] = new KeyValuePair<string, PropertyValue>(key, value);
		}
	}

	public bool Remove(KeyValuePair<string, PropertyValue> keyValuePair)
	{
		string text = ParseIndexedKey(keyValuePair.Key);
		if (text == null)
		{
			return false;
		}
		keyValuePair = new KeyValuePair<string, PropertyValue>(text, keyValuePair.Value);
		return m_tileSheet.Properties.Remove(keyValuePair);
	}

	public IEnumerator<KeyValuePair<string, PropertyValue>> GetEnumerator()
	{
		List<KeyValuePair<string, PropertyValue>> list = new List<KeyValuePair<string, PropertyValue>>();
		foreach (string key in Keys)
		{
			PropertyValue value = this[key];
			KeyValuePair<string, PropertyValue> item = new KeyValuePair<string, PropertyValue>(key, value);
			list.Add(item);
		}
		return list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
