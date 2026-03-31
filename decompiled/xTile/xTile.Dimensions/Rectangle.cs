namespace xTile.Dimensions;

public struct Rectangle
{
	public Location Location;

	public Size Size;

	public int X
	{
		get
		{
			return Location.X;
		}
		set
		{
			Location.X = value;
		}
	}

	public int Y
	{
		get
		{
			return Location.Y;
		}
		set
		{
			Location.Y = value;
		}
	}

	public int Width
	{
		get
		{
			return Size.Width;
		}
		set
		{
			Size.Width = value;
		}
	}

	public int Height
	{
		get
		{
			return Size.Height;
		}
		set
		{
			Size.Height = value;
		}
	}

	public Location MaxCorner => new Location(Location.X + Size.Width - 1, Location.Y + Size.Height - 1);

	public Rectangle(Location location, Size size)
	{
		Location = location;
		Size = size;
	}

	public Rectangle(Size size)
	{
		Location = Location.Origin;
		Size = size;
	}

	public Rectangle(int x, int y, int width, int height)
	{
		Location = new Location(x, y);
		Size = new Size(width, height);
	}

	public Rectangle(Rectangle rectangle)
	{
		Location = rectangle.Location;
		Size = rectangle.Size;
	}

	public bool Contains(Location location)
	{
		if (location.X >= Location.X && location.Y >= Location.Y && location.X < Location.X + Size.Width)
		{
			return location.Y < Location.Y + Size.Height;
		}
		return false;
	}

	public bool Intersects(Rectangle rectangle)
	{
		if (Location.X + Size.Width > rectangle.Location.X && Location.Y + Size.Height > rectangle.Location.Y && Location.X < rectangle.Location.X + rectangle.Size.Width)
		{
			return Location.Y < rectangle.Location.Y + rectangle.Size.Height;
		}
		return false;
	}

	public void ExtendTo(Location location)
	{
		Location location2 = location - Location;
		if (location2.X < 0)
		{
			Location.X = location.X;
			Size.Width -= location2.X;
		}
		else if (Size.Width <= location2.X)
		{
			Size.Width = location2.X + 1;
		}
		if (location2.Y < 0)
		{
			Location.Y = location.Y;
			Size.Height -= location2.Y;
		}
		else if (Size.Height <= location2.Y)
		{
			Size.Height = location2.Y + 1;
		}
	}

	public void ExtendTo(Rectangle rectangle)
	{
		Location location = rectangle.Location;
		ExtendTo(location);
		location.X += rectangle.Size.Width - 1;
		ExtendTo(location);
		location.Y += rectangle.Size.Height - 1;
		ExtendTo(location);
		location.X -= rectangle.Size.Width - 1;
		ExtendTo(location);
	}

	public override string ToString()
	{
		return Location.ToString() + " - " + Size.ToString();
	}
}
