using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace StardewValley.InternalBmFont;

public sealed class XmlSource
{
	public string Source { get; set; } = string.Empty;
}

public sealed class FontFile
{
	public FontCommon Common { get; set; } = new FontCommon();

	public List<FontChar> Chars { get; } = new List<FontChar>();

	public List<FontPage> Pages { get; } = new List<FontPage>();
}

public sealed class FontCommon
{
	public int LineHeight { get; set; } = 16;
}

public sealed class FontChar
{
	public int ID { get; set; }

	public int X { get; set; }

	public int Y { get; set; }

	public int Width { get; set; }

	public int Height { get; set; }

	public int XOffset { get; set; }

	public int YOffset { get; set; }

	public int XAdvance { get; set; }

	public int Page { get; set; }
}

public sealed class FontPage
{
	public int ID { get; set; }

	public string File { get; set; } = string.Empty;
}

public static class FontLoader
{
	public static FontFile Parse(string source)
	{
		if (string.IsNullOrWhiteSpace(source))
		{
			return new FontFile();
		}

		XDocument document = XDocument.Parse(source, LoadOptions.None);
		FontFile result = new FontFile();

		XElement? common = document.Root?.Element("common");
		if (common != null)
		{
			result.Common.LineHeight = ReadInt(common, "lineHeight", 16);
		}

		XElement? pages = document.Root?.Element("pages");
		if (pages != null)
		{
			foreach (XElement page in pages.Elements("page"))
			{
				result.Pages.Add(new FontPage
				{
					ID = ReadInt(page, "id", 0),
					File = ReadString(page, "file")
				});
			}
		}

		XElement? chars = document.Root?.Element("chars");
		if (chars != null)
		{
			foreach (XElement ch in chars.Elements("char"))
			{
				result.Chars.Add(new FontChar
				{
					ID = ReadInt(ch, "id", 0),
					X = ReadInt(ch, "x", 0),
					Y = ReadInt(ch, "y", 0),
					Width = ReadInt(ch, "width", 0),
					Height = ReadInt(ch, "height", 0),
					XOffset = ReadInt(ch, "xoffset", 0),
					YOffset = ReadInt(ch, "yoffset", 0),
					XAdvance = ReadInt(ch, "xadvance", 0),
					Page = ReadInt(ch, "page", 0)
				});
			}
		}

		return result;
	}

	private static int ReadInt(XElement element, string name, int fallback)
	{
		string? value = element.Attribute(name)?.Value;
		if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
		{
			return parsed;
		}

		return fallback;
	}

	private static string ReadString(XElement element, string name)
	{
		return element.Attribute(name)?.Value ?? string.Empty;
	}
}
