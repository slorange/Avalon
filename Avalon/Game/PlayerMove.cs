using System.Collections.Generic;
using System.Linq;

namespace Avalon.Game;

public class PlayerMove
{
	public LinkedList<PieceMove> Moves = new();
	public LinkedList<Piece> Captures = new();
	public int Test = -1;
	public Point? GhostLocation = null;
	public int Score = 0;

	public PlayerMove(Point from, Point to, Piece? capture)
	{
		Moves.AddLast(new PieceMove(from, to));
		if (capture != null)
			Captures.AddLast(capture);
	}

	public PlayerMove(Point from, Point to, Piece? capture, int test) : this(from, to, capture)
	{
		Test = test;
	}

	public PlayerMove(Point from, Point to, LinkedList<Piece> captures)
	{
		Moves.AddLast(new PieceMove(from, to));
		Captures = captures;
	}

	public PlayerMove(LinkedList<PieceMove> moves)
	{
		Moves = moves;
	}

	public PlayerMove(LinkedList<PieceMove> moves, LinkedList<Piece> captures)
	{
		Moves = moves;
		Captures = captures;
	}

	public bool HasCaptures() => Captures.Count > 0;

	public static PlayerMove? GetFirstMove(LinkedList<PlayerMove> moves, Point p)
	{
		return moves.FirstOrDefault(m => m.Moves.First().To.Equals(p));
	}

	public static PlayerMove Merge(PlayerMove m1, PlayerMove m2)
	{
		var captures = new LinkedList<Piece>(m1.Captures.Concat(m2.Captures));
		var moves = new LinkedList<PieceMove>(m1.Moves.Concat(m2.Moves));

		moves.Remove(m1.Moves.First());
		moves.Remove(m2.Moves.First());
		moves.AddLast(PieceMove.Merge(m1.Moves.First(), m2.Moves.First()));

		return new PlayerMove(moves, captures);
	}

	public override bool Equals(object? obj)
	{
		return obj is PlayerMove m && Moves.First() == m.Moves.First();
	}
}

public class PieceMove
{
	public Point From { get; }
	public Point To { get; }

	public PieceMove(Point from, Point to)
	{
		From = from;
		To = to;
	}

	public static PieceMove Merge(PieceMove m1, PieceMove m2)
	{
		return new PieceMove(m1.From, m2.To);
	}

	public override bool Equals(object? obj)
	{
		return obj is PieceMove m && From == m.From && To == m.To;
	}
}
