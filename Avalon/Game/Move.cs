using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalon.Game;

public class Move
{
	public enum MoveType { Slide, Leap, Hop, Locust, Other }
	public enum CaptureType { All, CaptureOnly, MoveOnly }

	public LinkedList<Point> Direction = new();
	public int Distance = 1;
	public MoveType Type = MoveType.Other;
	public CaptureType Capture = CaptureType.All;
	public bool Bounce = false;
	public bool FirstOnly = false;
	public string? CaptureTargetName = null;
	public int CaptureMaxValue = -1;
	public LinkedList<Point>? CaptureLocations = null;
	public Point? RelativeGhostLocation = null;
	public Point? AbsoluteGhostLocation = null;
	public bool GhostEater = false;
	public LinkedList<Point> Cleave = new();

	//TODOs
	//Value based capture
	//capture piece not on square
	//twopiece (castling)
	//alternate

	public Move() { }

	public Move(MoveType type, LinkedList<Point> direction, int distance)
	{
		Type = type;
		Direction = direction;
		Distance = distance;
	}

	public virtual LinkedList<PlayerMove> GetValidMoves(Piece piece, Board board, LinkedList<Point> dependencies, bool reverseDirection = false)
	{
		var moves = new LinkedList<PlayerMove>();
		if (Stunned(piece, board, dependencies)) return moves;
		if (Type == MoveType.Other) return moves;
		if (FirstOnly && piece.Moved) return moves;

		var l = new Point(piece.X, piece.Y);
		AbsoluteGhostPosition(l, reverseDirection);

		foreach (var io in Direction)
		{
			var i = new Point(io);
			if (reverseDirection) i = new Point(i.X, -i.Y);
			Piece? leaptOver = null;
			var p = new Point(l);

			for (int j = 1; j <= Distance; j++)
			{
				if (Bounce) //needs debugging
				{
					int offBoard = board.OnBoardX(p.X + i.X);
					if (offBoard != 0)
					{
						int inBoard = Math.Abs(i.X) - offBoard;
						p.X += i.X / Math.Abs(i.X) * inBoard * 2;
						i.X = -i.X;
					}
					offBoard = board.OnBoardY(p.Y + i.Y);
					if (offBoard != 0)
					{
						int inBoard = Math.Abs(i.Y) - offBoard;
						p.Y += i.Y / Math.Abs(i.Y) * inBoard * 2;
						i.Y = -i.Y;
					}
				}

				p = Point.Add(p, i);
				if (board.OnBoard(p) != 0) break;
				dependencies.AddLast(new Point(p));

				var other = board.GetPiece(p);
				if (GhostEater && p.Equals(board.GhostLocation)) other = board.LastMoved;

				if (Type == MoveType.Slide)
				{
					if (other != null && other.Color == piece.Color) break;
					//int tmp = (int)((Math.atan(((double)io.y)/io.x)/(Math.PI/2)+1)*2);
					moves.AddLast(NewMove(piece, board, p, other, dependencies));
					if (other != null) break;
				}
				else if (Type == MoveType.Hop)
				{
					if (other != null && other.Color == piece.Color) continue;
					moves.AddLast(NewMove(piece, board, p, other, dependencies));
				}
				else if (Type == MoveType.Leap)
				{
					if (leaptOver != null)
					{
						if (other != null && other.Color == piece.Color) break;
						moves.AddLast(NewMove(piece, board, p, other, dependencies));
						break;
					}
					if (other == null) continue;
					leaptOver = other;
				}
				else if (Type == MoveType.Locust)
				{
					if (leaptOver != null)
					{
						if (other != null) break;
						moves.AddLast(NewMove(piece, board, p, leaptOver, dependencies));
						break;
					}
					if (other == null) continue;
					if (other.Color == piece.Color) break;
					leaptOver = other;
					break;
				}
			}
		}

		//TODO capture everything and sort out your pieces here (maybe)

		if (Capture == CaptureType.All) return moves;

		var filteredMoves = new LinkedList<PlayerMove>();
		foreach (var move in moves)
		{
			if ((Capture == CaptureType.MoveOnly) ^ move.HasCaptures())
				filteredMoves.AddLast(move);
		}
		return filteredMoves;
	}

	private bool Stunned(Piece piece, Board board, LinkedList<Point> dependencies)
	{
		var ps = new List<Point>
		{
			new Point(piece.X - 1, piece.Y),
			new Point(piece.X + 1, piece.Y),
			new Point(piece.X, piece.Y - 1),
			new Point(piece.X, piece.Y + 1)
		};

		foreach (var p in ps)
		{
			if (board.OnBoard(p) == 0)
			{
				var p1 = board.GetPiece(p);
				if (p1 != null && p1.Stunner && p1.Color != piece.Color)
				{
					dependencies.AddLast(p);
					return true;
				}
			}
		}
		return false;
	}

	private void AbsoluteGhostPosition(Point l, bool reverseDirection)
	{
		if (RelativeGhostLocation != null)
		{
			var tmp = new Point(RelativeGhostLocation);
			if (reverseDirection) tmp.Y = -tmp.Y;
			AbsoluteGhostLocation = Point.Add(l, tmp);
		}
	}

	private PlayerMove NewMove(Piece piece, Board board, Point to, Piece? capture, LinkedList<Point> dependencies)
	{
		var captures = new LinkedList<Piece>();
		if (capture != null) captures.AddLast(capture);

		foreach (var p in Cleave)
		{
			var p2 = Point.Add(to, p);
			if (board.OnBoard(p2) != 0) continue;
			dependencies.AddLast(p2);
			var other = board.GetPiece(p2);
			if (other != null && other.Color != piece.Color) captures.AddLast(other);
		}

		var move = new PlayerMove(piece.Location(), new Point(to), captures) { GhostLocation = AbsoluteGhostLocation };
		return move;
	}

	public bool CanCapture() => Capture != CaptureType.MoveOnly;
	public bool CanMove() => Capture != CaptureType.CaptureOnly;
	public bool Slide() => Type == MoveType.Slide;
	public bool Hop() => Type == MoveType.Hop;
	public bool Leap() => Type == MoveType.Leap;
	public bool Locust() => Type == MoveType.Locust;

	public static LinkedList<Point> GetVertical() =>
	new(new[] { new Point(0, 1), new Point(0, -1) });
	public static LinkedList<Point> GetHorizontal() =>
	new(new[] { new Point(1, 0), new Point(-1, 0) });

	public static LinkedList<Point> GetOrthogonal() =>
		new(new[] { new Point(1, 0), new Point(0, 1), new Point(0, -1), new Point(-1, 0) });

	public static LinkedList<Point> GetDiagonal() =>
		new(new[] { new Point(1, 1), new Point(-1, -1), new Point(1, -1), new Point(-1, 1) });

	public static LinkedList<Point> GetAllEight() =>
		new(new[] { new Point(1, 0), new Point(0, 1), new Point(0, -1), new Point(-1, 0),
					new Point(1, 1), new Point(-1, -1), new Point(1, -1), new Point(-1, 1) });

	public static LinkedList<Point> GetKnightMoves(int x, int y) =>
		new(new[] { new Point(x, y), new Point(y, x), new Point(x, -y), new Point(y, -x),
					new Point(-x, -y), new Point(-y, -x), new Point(-x, y), new Point(-y, x) });

	public static LinkedList<Point> Multiply(LinkedList<Point> points, int m) =>
		new LinkedList<Point>(points.Select(p => Point.Multiply(p, m)));
}

class TwoPartMove : Move
{
	Move M1, M2;
	bool Multikill;

	public TwoPartMove(Move m1, Move m2, bool multikill)
	{
		M1 = m1;
		M2 = m2;
		Multikill = multikill;
	}

	public override LinkedList<PlayerMove> GetValidMoves(Piece piece, Board board, LinkedList<Point> dependencies, bool reverseDirection)
	{
		var moves1 = M1.GetValidMoves(piece, board, dependencies, reverseDirection);
		var totalMoves = new HashSet<PlayerMove>();

		foreach (var move in moves1)
		{
			var copy = board.CopyBoard();
			copy.Move(move);
			var loc = move.Moves.First().To;
			var p2 = copy.GetPiece(loc.X, loc.Y);

			var moves2 = M2.GetValidMoves(p2, copy, dependencies, reverseDirection);
			totalMoves.Add(move);

			if (Multikill || !move.HasCaptures())
			{
				foreach (var move2 in moves2)
				{
					totalMoves.Add(PlayerMove.Merge(move, move2));
				}
			}
		}

		return new LinkedList<PlayerMove>(totalMoves);
	}
}

class InfiniteMove : Move
{
	Move M;
	bool Multikill;
	Move MultiMove;

	public InfiniteMove(Move m, bool multikill)
	{
		M = m;
		Multikill = multikill;
		MultiMove = new TwoPartMove(m, this, multikill);
	}

	public override LinkedList<PlayerMove> GetValidMoves(Piece piece, Board board, LinkedList<Point> dependencies, bool reverseDirection)
	{
		if (M.GetValidMoves(piece, board, dependencies, reverseDirection).Count == 0)
			return new LinkedList<PlayerMove>();

		return MultiMove.GetValidMoves(piece, board, dependencies, reverseDirection);
	}
}

class TwoPieceMove
{
}

class CopyLastMove : Move
{
	public override LinkedList<PlayerMove> GetValidMoves(Piece piece, Board board, LinkedList<Point> dependencies, bool reverseDirection = false)
	{
		var last = board.LastCopyableMove;
		if (last == null) return new LinkedList<PlayerMove>();

		var moveList = last.MoveList;

		/* TODO could make joker promote if it reaches the end as a pawn
		//it's close, the color is off which is hard to fix right now
		piece.promotion = last.promotion;
		if(piece.promotion != null){
			piece.promotion.pieces.getFirst().
		}*/

		var moves = new LinkedList<PlayerMove>();

		foreach (var m in moveList)
		{
			var newMoves = m.GetValidMoves(piece, board, dependencies, last.Color != piece.Color);
			foreach (var m2 in newMoves)
			{
				moves.AddFirst(m2);
			}
		}

		foreach (var p in board.GetPieces(Board.OtherColor(piece.Color)))
		{
			dependencies.AddLast(p.Location());
		}

		return moves;
	}
}

class AlternateMove
{
}
