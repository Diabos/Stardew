namespace xTile.Format;

public class CompatibilityNote
{
	private CompatibilityLevel m_compatibilityLevel;

	private string m_remarks;

	public CompatibilityLevel CompatibilityLevel => m_compatibilityLevel;

	public string Remarks => m_remarks;

	public CompatibilityNote(CompatibilityLevel compatibilityLevel, string remarks)
	{
		m_compatibilityLevel = compatibilityLevel;
		m_remarks = remarks;
	}
}
