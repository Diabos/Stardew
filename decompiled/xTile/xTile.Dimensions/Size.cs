using System;

namespace xTile.Dimensions;

public struct Size
{
	public int Width;

	public int Height;

	private static Size s_sizeZero = new Size(0, 0);

	public static Size Zero => s_sizeZero;

	public int Area => Width * Height;

	public bool Square => Width == Height;

	public static Size FromString(string value)
	{
		string[] array = value.Split(new char[1] { 'x' });
		if (array.Length != 2)
		{
			throw new FormatException("Size string format must be in the form 'N x N'");
		}
		return new Size(int.Parse(array[0]), int.Parse(array[1]));
	}

	public static bool operator ==(Size size1, Size size2)
	{
		if (size1.Width == size2.Width)
		{
			return size1.Height == size2.Height;
		}
		return false;
	}

	public static bool operator !=(Size size1, Size size2)
	{
		if (size1.Width == size2.Width)
		{
			return size1.Height != size2.Height;
		}
		return true;
	}

	public Size(int width, int height)
	{
		Width = width;
		Height = height;
	}

	public Size(int size)
	{
		Height = size;
		Width = size;
	}

	public override bool Equals(object other)
	{
		if (!(other is Size size))
		{
			return false;
		}
		return Width == size.Width && Height == size.Height;
	}

	public override int GetHashCode()
	{
		return Width + Height;
	}

	public override string ToString()
	{
		return Width + " x " + Height;
	}
}
