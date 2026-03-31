using System.Collections;
using System.Collections.Generic;

namespace xTile.ObjectModel;

public interface IPropertyCollection : IDictionary<string, PropertyValue>, ICollection<KeyValuePair<string, PropertyValue>>, IEnumerable<KeyValuePair<string, PropertyValue>>, IEnumerable
{
	void CopyFrom(IPropertyCollection propertyCollection);
}
