namespace xTile.Dimensions;

public struct Location
{
	public int X;

	public int Y;

	private static Location s_origin = new Location(0, 0);

	public static Location Origin => s_origin;

	public Location AboveLeft => new Location(X - 1, Y - 1);

	public Location Above => new Location(X, Y - 1);

	public Location AboveRight => new Location(X + 1, Y - 1);

	public Location Left => new Location(X - 1, Y);

	public Location Right => new Location(X + 1, Y);

	public Location BelowLeft => new Location(X - 1, Y + 1);

	public Location Below => new Location(X, Y + 1);

	public Location BelowRight => new Location(X + 1, Y + 1);

	public static bool operator ==(Location location1, Location location2)
	{
		if (location1.X == location2.X)
		{
			return location1.Y == location2.Y;
		}
		return false;
	}

	public static bool operator !=(Location location1, Location location2)
	{
		if (location1.X == location2.X)
		{
			return location1.Y != location2.Y;
		}
		return true;
	}

	public static Location operator -(Location location)
	{
		return new Location(-location.X, -location.Y);
	}

	public static Location operator +(Location location1, Location location2)
	{
		return new Location(location1.X + location2.X, location1.Y + location2.Y);
	}

	public static Location operator -(Location location1, Location location2)
	{
		return new Location(location1.X - location2.X, location1.Y - location2.Y);
	}

	public static Location operator *(Location location, int scale)
	{
		return new Location(location.X * scale, location.Y * scale);
	}

	public static Location operator *(int scale, Location location)
	{
		return new Location(location.X * scale, location.Y * scale);
	}

	public static Location operator /(Location location, int divisor)
	{
		return new Location(location.X / divisor, location.Y / divisor);
	}

	public Location(int x, int y)
	{
		X = x;
		Y = y;
	}

	public override int GetHashCode()
	{
		return X + Y;
	}

	public override bool Equals(object obj)
	{
		if (!(obj is Location location))
		{
			return false;
		}
		return X == location.X && Y == location.Y;
	}

	public override string ToString()
	{
		return "[" + X + ", " + Y + "]";
	}
}
