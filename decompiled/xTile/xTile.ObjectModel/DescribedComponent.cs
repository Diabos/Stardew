namespace xTile.ObjectModel;

public abstract class DescribedComponent : Component
{
	private string m_description;

	public string Description
	{
		get
		{
			return m_description;
		}
		set
		{
			m_description = value;
		}
	}

	public DescribedComponent()
	{
		m_description = "";
	}

	public DescribedComponent(string id)
		: base(id)
	{
		m_description = "";
	}

	public DescribedComponent(string id, string description)
		: base(id)
	{
		m_description = description;
	}
}
