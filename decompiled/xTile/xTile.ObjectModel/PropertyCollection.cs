using System.Collections;
using System.Collections.Generic;

namespace xTile.ObjectModel;

public class PropertyCollection : Dictionary<string, PropertyValue>, IPropertyCollection, IDictionary<string, PropertyValue>, ICollection<KeyValuePair<string, PropertyValue>>, IEnumerable<KeyValuePair<string, PropertyValue>>, IEnumerable
{
	public PropertyCollection()
	{
	}

	public PropertyCollection(IPropertyCollection propertyCollection)
		: base(propertyCollection.Count)
	{
		CopyFrom(propertyCollection);
	}

	public void CopyFrom(IPropertyCollection propertyCollection)
	{
		foreach (KeyValuePair<string, PropertyValue> item in propertyCollection)
		{
			base[item.Key] = new PropertyValue(item.Value);
		}
	}
}
