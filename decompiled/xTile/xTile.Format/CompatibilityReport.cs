using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace xTile.Format;

public class CompatibilityReport
{
	private List<CompatibilityNote> m_compatibilityNotes;

	public CompatibilityLevel CompatibilityLevel
	{
		get
		{
			CompatibilityLevel result = CompatibilityLevel.Full;
			using List<CompatibilityNote>.Enumerator enumerator = m_compatibilityNotes.GetEnumerator();
			while (enumerator.MoveNext())
			{
				switch (enumerator.Current.CompatibilityLevel)
				{
				case CompatibilityLevel.Partial:
					result = CompatibilityLevel.Partial;
					break;
				case CompatibilityLevel.None:
					return CompatibilityLevel.None;
				}
			}
			return result;
		}
	}

	public ReadOnlyCollection<CompatibilityNote> CompatibilityNotes => m_compatibilityNotes.AsReadOnly();

	public CompatibilityReport(IEnumerable<CompatibilityNote> compatibilityNotes)
	{
		m_compatibilityNotes = new List<CompatibilityNote>(compatibilityNotes);
	}

	public CompatibilityReport()
	{
		m_compatibilityNotes = new List<CompatibilityNote>();
	}
}
