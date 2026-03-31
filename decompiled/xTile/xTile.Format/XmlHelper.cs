using System;
using System.Xml;

namespace xTile.Format;

public class XmlHelper
{
	private XmlReader m_xmlReader;

	public XmlReader XmlReader => m_xmlReader;

	public XmlHelper(XmlReader xmlReader)
	{
		m_xmlReader = xmlReader;
	}

	public XmlNodeType AdvanceNode()
	{
		if (!m_xmlReader.Read())
		{
			throw new Exception("End of XML stream reached");
		}
		return m_xmlReader.NodeType;
	}

	public void AdvanceNode(XmlNodeType xmlNodeType)
	{
		AdvanceNode();
		if (m_xmlReader.NodeType != xmlNodeType)
		{
			throw new Exception("The expected node is " + xmlNodeType.ToString() + " but found " + m_xmlReader.NodeType.ToString() + " named " + m_xmlReader.Name);
		}
	}

	public void AdvanceNamedNode(XmlNodeType xmlNodeType, string nodeName)
	{
		AdvanceNode(xmlNodeType);
		if (m_xmlReader.Name != nodeName)
		{
			throw new Exception(string.Concat("Node '", nodeName, "' of type '", xmlNodeType, "' expected", "found", m_xmlReader.Name));
		}
	}

	public void AdvanceDeclaration()
	{
		AdvanceNode(XmlNodeType.XmlDeclaration);
	}

	public void AdvanceStartElement(string elementName)
	{
		AdvanceNamedNode(XmlNodeType.Element, elementName);
	}

	public bool AdvanceStartRepeatedElement(string elementName, string closingContainerName)
	{
		if (m_xmlReader.NodeType == XmlNodeType.Element && m_xmlReader.IsEmptyElement && m_xmlReader.Name == closingContainerName)
		{
			return false;
		}
		XmlNodeType xmlNodeType = AdvanceNode();
		switch (xmlNodeType)
		{
		case XmlNodeType.EndElement:
			if (!(m_xmlReader.Name == closingContainerName))
			{
				throw new Exception("Expected closing element '" + closingContainerName + "'");
			}
			return false;
		case XmlNodeType.Element:
			if (xmlNodeType != XmlNodeType.Element || !(m_xmlReader.Name != elementName))
			{
				break;
			}
			goto default;
		default:
			throw new Exception("Repeated element '" + elementName + "' or closing container element expected");
		}
		return true;
	}

	public void AdvanceEndElement(string elementName)
	{
		if (!m_xmlReader.IsEmptyElement || !(m_xmlReader.Name == elementName))
		{
			AdvanceNamedNode(XmlNodeType.EndElement, elementName);
		}
	}

	public void SkipToEndElement(string elementName)
	{
		while (m_xmlReader.NodeType != XmlNodeType.EndElement || m_xmlReader.Name != elementName)
		{
			AdvanceNode();
		}
	}

	public string GetAttribute(string attributeName)
	{
		if (!m_xmlReader.HasAttributes)
		{
			throw new Exception(string.Concat("Node '", m_xmlReader.Name, "' of type '", m_xmlReader.NodeType, "' has no attributes"));
		}
		return m_xmlReader.GetAttribute(attributeName) ?? throw new Exception("No attribute '" + attributeName + "' defined");
	}

	public string GetAttribute(string attributeName, string defaultValue)
	{
		string attribute = m_xmlReader.GetAttribute(attributeName);
		if (attribute != null)
		{
			return attribute;
		}
		return defaultValue;
	}

	public int GetIntAttribute(string attributeName)
	{
		string attribute = GetAttribute(attributeName);
		try
		{
			return int.Parse(attribute);
		}
		catch (Exception innerException)
		{
			throw new Exception("Attribute '" + attribute + "' is not a valid integer", innerException);
		}
	}

	public int GetIntAttribute(string attributeName, int defaultValue)
	{
		string attribute = m_xmlReader.GetAttribute(attributeName);
		if (attribute == null)
		{
			return defaultValue;
		}
		try
		{
			return int.Parse(attribute);
		}
		catch (Exception innerException)
		{
			throw new Exception("Attribute '" + attribute + "' is not a valid integer", innerException);
		}
	}

	public string GetCData()
	{
		AdvanceNode(XmlNodeType.CDATA);
		return m_xmlReader.Value;
	}
}
