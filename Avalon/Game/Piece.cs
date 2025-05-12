using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace Avalon.Game;

public class Piece
{
	public static Dictionary<string, IImage> AllImages = new Dictionary<string, IImage>();

	public int X, Y;
	public char Color;
	public string Name;
	public IImage Image;
	public Board Board;
	public bool Moved;
	public int Value;
	public LinkedList<Move> MoveList;
	public Promotion Promotion;
	public bool Royalty;
	public bool Stunner;

	public LinkedList<Point> Dependencies = new();
	public LinkedList<PlayerMove> ValidMoves;
	public bool Valid = false;
	public int C = 0;

	//TODO MoveRange
	//TODO Bodyguard

	public static IImage LoadImage(char color, string name)
	{
		string id = (""+color + name).ToLower();
		if (AllImages.ContainsKey(id)) return AllImages[id];
		return new Bitmap(AssetLoader.Open(new Uri($"avares://Avalon/Assets/Pieces/{id}.png")));
	}

	public Piece(string name, int value, LinkedList<Move> moveList, bool royalty = false)
	{
		Name = name;
		Value = value;
		MoveList = moveList;
		Royalty = royalty;
		Stunner = name == "Z";
	}

	public Piece(char color, string name, int value, Board board, int x, int y, LinkedList<Move> moveList, bool royalty = false)
		: this(name, value, moveList, royalty)
	{       
		Image = LoadImage(color, name);
		Color = color;
		X = x;
		Y = y;
		Board = board;
		Moved = false;
		Dependencies = new LinkedList<Point>();
	}

	public Piece(Piece copy, Board b) : this(copy.Color, copy.Name, copy.Value, b, copy.X, copy.Y, copy.MoveList, copy.Royalty)
	{
		Board = b;
		Moved = copy.Moved;
		Promotion = copy.Promotion;
		Valid = copy.Valid;
		C = copy.C;
		if (Valid)
		{
			Dependencies = copy.Dependencies;
			ValidMoves = copy.ValidMoves;
		}
		Board.AddDep(this, C, Dependencies);
	}

	public void ApplyMove(Point p) => ApplyMove(p.X, p.Y);

	public void ApplyMove(int x, int y)
	{
		X = x;
		Y = y;
		Moved = true;
		Invalidate(C);
		_promoValue = -1;

		if (Promotion != null && DistanceToPromotion() == 0)
		{
			Promote();
		}
	}

	public int DistanceToPromotion()
	{
		if (Promotion == null) return -1;
		int dir = Board.GetForwardDirection(Color).Y;
		return dir == 1 ? Math.Max(Board.SIZE - Promotion.Rows - Y, 0) : Math.Max(Y - Promotion.Rows + 1, 0);
	}

	private double _promoValue = -1;
	public double GetPromoValue()
	{
		if (_promoValue != -1) return _promoValue;
		if (Promotion != null) //TODO do this in the piece move method
		{
			int diff = Promotion.Pieces.First().Value - Value;
			_promoValue = diff / Math.Pow(2, DistanceToPromotion());
		}
		return _promoValue;
	}

	public int GetValue()
	{
		int v = Value + (int)GetPromoValue();
		if (Royalty) v += 1000000;
		//v += getValidMoves().size();//TODO remove duplicates
		return v;
	}

	public void Promote()
	{
		if (Promotion == null) return;
		var p = Promotion.Pieces.First();
		Name = p.Name;
		Image = LoadImage(Color, Name);
		Value = p.Value;
		MoveList = p.MoveList;
		Promotion = p.Promotion;
	}

	public HashSet<Piece> InCheck()
	{
		var dangers = new HashSet<Piece>();
		foreach (var p in Board.GetPieces(Board.OtherColor(Color)))
		{
			foreach (var m in p.GetValidMoves(false))
			{
				if (m.Captures.Contains(this))
					dangers.Add(p);
			}
		}
		return dangers;
	}

	public Point Location() => new Point(X, Y);

	// If check is false then it returns all moves
	// If check is true then it only returns moves that don't leave royalty in check
	public LinkedList<PlayerMove> GetValidMoves(bool check)
	{
		if (!Valid) Validate();
		if (!check) return ValidMoves;

		var stillValid = new LinkedList<PlayerMove>();
		foreach (var m in ValidMoves)
		{
			var test = Board.TestMove(m);
			if (!test.InCheck(Color))
				stillValid.AddLast(m);
		}
		return stillValid;
	}

	public void Validate()
	{
		Dependencies.Clear();
		ValidMoves = GetValidMoves(this, MoveList, Board, Dependencies);
		Board.AddDep(this, ++C, Dependencies);
		Valid = true;
	}

	private static LinkedList<PlayerMove> GetValidMoves(Piece piece, LinkedList<Move> moveList, Board board, LinkedList<Point> dependencies)
	{
		var moves = new HashSet<PlayerMove>();
		foreach (var m in moveList)
		{
			moves.UnionWith(m.GetValidMoves(piece, board, dependencies));
		}
		return new LinkedList<PlayerMove>(moves);
	}

	public void Invalidate(int i)
	{
		if (i == C) Valid = false;
	}

	public override string ToString() => $"{Color}{Name}";

	public static LinkedList<Move> GetPawnMoves(char color)
	{
		var moveList = new LinkedList<Move>();

		Point forward = Board.GetForwardDirection(color);

		var direction = new LinkedList<Point>();
		direction.AddFirst(forward);
		var m = new Move(Move.MoveType.Slide, direction, 1)
		{
			Capture = Move.CaptureType.MoveOnly
		};
		moveList.AddLast(m);

		direction = new LinkedList<Point>();
		direction.AddFirst(forward);
		m = new Move(Move.MoveType.Slide, direction, 2)
		{
			FirstOnly = true,
			Capture = Move.CaptureType.MoveOnly,
			RelativeGhostLocation = forward
		};
		moveList.AddLast(m);

		direction = new LinkedList<Point>();
		direction.AddFirst(Point.Add(forward, new Point(1, 0)));
		direction.AddFirst(Point.Add(forward, new Point(-1, 0)));
		m = new Move(Move.MoveType.Slide, direction, 1)
		{
			Capture = Move.CaptureType.CaptureOnly,
			GhostEater = true
		};
		moveList.AddLast(m);

		return moveList;
	}

	public static LinkedList<Move> GetKnightMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetKnightMoves(1, 2), 1));
		return moveList;
	}

	public static LinkedList<Move> GetBishopMoves(Board b)
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetDiagonal(), b.SIZE));
		return moveList;
	}

	public static LinkedList<Move> GetRookMoves(Board b)
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetOrthogonal(), b.SIZE));
		return moveList;
	}

	public static LinkedList<Move> GetQueenMoves(Board b)
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetAllEight(), b.SIZE));
		return moveList;
	}

	public static LinkedList<Move> GetKingMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetAllEight(), 1));
		return moveList;
	}

	public static LinkedList<Move> GetGryphonMoves(Board b)
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(
			new TwoPartMove(
				new Move(Move.MoveType.Slide, Move.GetDiagonal(), 1),
				new Move(Move.MoveType.Slide, Move.GetOrthogonal(), b.SIZE),
				false
			));
		return moveList;
	}

	public static LinkedList<Move> GetAmazonMoves(Board b)
	{
		var moveList = GetQueenMoves(b);
		foreach (var move in GetNightRiderMoves(b))
			moveList.AddLast(move);
		return moveList;
	}

	public static LinkedList<Move> GetPaladinMoves(Board b)
	{
		var moveList = GetBishopMoves(b);
		moveList.AddLast(new Move(Move.MoveType.Slide, Move.GetKnightMoves(1, 2), b.SIZE));
		return moveList;
	}

	public static LinkedList<Move> GetNightRiderMoves(Board b)
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetKnightMoves(1, 2), b.SIZE));
		return moveList;
	}

	public static LinkedList<Move> GetWizardMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetKnightMoves(1, 3), 1));
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetDiagonal(), 1));
		return moveList;
	}

	public static LinkedList<Move> GetChampionMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Hop, Move.Multiply(Move.GetDiagonal(), 2), 1));
		moveList.AddFirst(new Move(Move.MoveType.Hop, Move.GetOrthogonal(), 2));
		return moveList;
	}

	public static LinkedList<Move> GetBeastMoves()
	{
		var moveList = new LinkedList<Move>();

		var m = new Move(Move.MoveType.Hop, Move.GetKnightMoves(1, 2), 2)
		{
			Cleave = Move.GetAllEight()
		};
		moveList.AddLast(m);

		var m2 = new Move(Move.MoveType.Hop, Move.Multiply(Move.GetOrthogonal(), 4), 1)
		{
			Cleave = Move.GetAllEight()
		};
		moveList.AddLast(m2);

		return moveList;
	}

	public static LinkedList<Move> GetCheckerMoves(char color)
	{
		var moveList = new LinkedList<Move>();

		var direction = new LinkedList<Point>();
		direction.AddFirst(Point.Add(Board.GetForwardDirection(color), new Point(-1, 0)));
		direction.AddFirst(Point.Add(Board.GetForwardDirection(color), new Point(1, 0)));

		var m = new Move(Move.MoveType.Locust, direction, 2)
		{
			Capture = Move.CaptureType.CaptureOnly
		};
		moveList.AddLast(new InfiniteMove(m, true));

		var m2 = new Move(Move.MoveType.Slide, direction, 1)
		{
			Capture = Move.CaptureType.MoveOnly
		};
		moveList.AddLast(m2);

		return moveList;
	}

	public static LinkedList<Move> GetPromotedCheckerMoves()
	{
		var moveList = new LinkedList<Move>();

		var m = new Move(Move.MoveType.Locust, Move.GetDiagonal(), 2)
		{
			Capture = Move.CaptureType.CaptureOnly
		};
		moveList.AddLast(new InfiniteMove(m, true));

		var m2 = new Move(Move.MoveType.Slide, Move.GetDiagonal(), 1)
		{
			Capture = Move.CaptureType.MoveOnly
		};
		moveList.AddLast(m2);

		return moveList;
	}

	public static LinkedList<Move> GetJokerMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new CopyLastMove());
		return moveList;
	}

	public static LinkedList<Move> GetPrincessMoves()
	{
		var moveList = new LinkedList<Move>();
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetOrthogonal(), 1) { Capture = Move.CaptureType.MoveOnly });
		moveList.AddFirst(new Move(Move.MoveType.Leap, Move.GetAllEight(), 2) { Capture = Move.CaptureType.MoveOnly });
		moveList.AddFirst(new Move(Move.MoveType.Slide, Move.GetHorizontal(), 8)
		{
			FirstOnly = true,
			Capture = Move.CaptureType.MoveOnly,
		});
		moveList.AddFirst(new Move(Move.MoveType.Leap, Move.GetHorizontal(), 8)
		{
			FirstOnly = true,
			Capture = Move.CaptureType.MoveOnly,
		});
		return moveList;
	}
}

public class Promotion
{
	public LinkedList<Piece> Pieces;
	public int Rows = 1;

	public Promotion(Piece p)
	{
		Pieces = new LinkedList<Piece>();
		Pieces.AddLast(p);
	}

	public Promotion(LinkedList<Piece> pieces)
	{
		Pieces = pieces;
	}

	public Promotion(Piece p, int rows) : this(p)
	{
		Rows = rows;
	}

	public Promotion(LinkedList<Piece> pieces, int rows) : this(pieces)
	{
		Rows = rows;
	}
}