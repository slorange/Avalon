using System;

namespace Avalon.Game;
public class Point
{
	public int X { get; set; } = 0;
	public int Y { get; set; } = 0;

	public Point() { }

	public Point(int x, int y)
	{
		X = x;
		Y = y;
	}

	public Point(Point p)
	{
		X = p.X;
		Y = p.Y;
	}

	public override string ToString() => $"({X},{Y})";

	public Point GetHorizontal() => new Point(X, 0);

	public Point GetVertical() => new Point(0, Y);

	public void Multiply(int m)
	{
		X *= m;
		Y *= m;
	}

	public void Add(Point p2)
	{
		X += p2.X;
		Y += p2.Y;
	}

	public static Point Multiply(Point p1, int m) => new Point(p1.X * m, p1.Y * m);

	public static Point Add(Point p1, Point p2) => new Point(p1.X + p2.X, p1.Y + p2.Y);

	public override bool Equals(object? obj)
	{
		return obj is Point p && X == p.X && Y == p.Y;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}
}