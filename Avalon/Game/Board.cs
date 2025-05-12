using Avalon.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalon.Game;

public class Board
{
	public int SIZE = 8;
	public bool AIOn = true;
	public enum GameMode { Chess, Checkers, FantasySmall, FantasyLarge }
	public GameMode Mode = GameMode.FantasySmall;

	private static ChessControl? ui;

	//test board is used by AI to try moves
	//some code isn't executed by test board to speed up AI

	public Piece?[,] board;
	public LinkedList<Piece> P1Captured = new();
	public LinkedList<Piece> P2Captured = new();
	public LinkedList<PlayerMove> ValidMoves = new();
	public List<Piece> CheckPieces = new List<Piece>();
	public Piece? SelectedPiece;
	public Piece? LastMoved;
	public Piece? LastCopyableMove;
	public Point? GhostLocation = null;
	public bool Test = true;
	public char Turn;

	public int AILEVEL = 2;

	private LinkedList<Dependent>[,] dependents;

	private class Dependent
	{
		public Piece Dep;
		public int C;
		public Dependent(Piece d, int c)
		{
			Dep = d;
			C = c;
		}
	}

	public void SetSize(int s)
	{
		SIZE = s;
		ui?.ChangeBoardSize(s); // Assuming you implement this in ChessControl later

		board = new Piece[s, s];
		dependents = new LinkedList<Dependent>[s, s];

		for (int x = 0; x < s; x++)
		{
			for (int y = 0; y < s; y++)
			{
				dependents[x, y] = new LinkedList<Dependent>();
			}
		}
	}

	private static Board mainInstance = new Board(true, GameMode.FantasySmall);

	public static Board StartNewGame(bool AI) => mainInstance = new Board(AI, mainInstance.Mode);
	public static Board StartNewGame(GameMode mode) => mainInstance = new Board(mainInstance.AIOn, mode);
	public static Board StartNewGame(bool AI, GameMode mode) => mainInstance = new Board(AI, mode);

	public static Board SetUI(ChessControl ui)
	{
		Board.ui = ui;
		return mainInstance;
	}

	private Board() { }

	private Board(bool AI, GameMode mode)
	{
		Mode = mode;
		AIOn = AI;
		Test = false;
		StartGameMode();
		Turn = 'W';
	}

	private void StartGameMode()
	{
		if (Mode == GameMode.Chess) NewChessGame();
		else if (Mode == GameMode.Checkers) NewCheckersGame();
		else if (Mode == GameMode.FantasySmall) TestGame();
		else NewGame();
	}

	public void Touch(int x, int y)
	{
		if (y < 0)
		{
			int n = CapturedPositionToInt('B', x, y);
			if (n < P2Captured.Count)
			{
				SelectedPiece = P2Captured.ElementAt(n);
				SelectedPiece.X = x;
				SelectedPiece.Y = y;
				DrawBoard();
			}
			return;
		}

		if (y >= SIZE)
		{
			int n = CapturedPositionToInt('W', x, y);
			if (n < P1Captured.Count)
			{
				SelectedPiece = P1Captured.ElementAt(n);
				SelectedPiece.X = x;
				SelectedPiece.Y = y;
				DrawBoard();
			}
			return;
		}

		SelectedPiece = null;
		if (AIOn && Turn == 'B') return;

		Point p = new Point(x, y);
		if (OnBoard(x, y) != 0) return;

		Piece? piece = board[x, y];
		if (piece != null && piece.Color == Turn) //first click
		{
			SelectedPiece = piece;
			ValidMoves = piece.GetValidMoves(true); //TODO for checkmate this was set to true when I came back
			DrawBoard();
		}
		else  //second click
		{
			PlayerMove? m = PlayerMove.GetFirstMove(ValidMoves, p);
			if (m == null) return;

			Move(m);
			ValidMoves.Clear();
			NextTurn();
			LookForCheck();
			DrawBoard();
			//if(b)
			//else {
			//	content = Button(text='OK')
			//	popup = Popup(title='Cannot put king into check', content=content, auto_dismiss=False)
			//	content.bind(on_press=popup.dismiss)
			//	popup.open()
			//}
		}
	}

	public int CapturedPositionToInt(char color, int x, int y)
	{
		return color == 'W' ? (y - SIZE) * SIZE + x : (-1 - y) * SIZE + x;
	}

	public Point CapturedIntToPosition(char color, int n)
	{
		return color == 'W'
			? new Point(n % SIZE, SIZE + n / SIZE)
			: new Point(n % SIZE, -1 - n / SIZE);
	}
	public Piece GetPiece(Point p)
	{
		return GetPiece(p.X, p.Y);
	}

	public Piece GetPiece(int x, int y)
	{
		return board[x,y];
	}

	public bool Move(PlayerMove move)
	{
		LinkedList<PieceMove> moves = move.Moves;
		LinkedList<Piece> captures = move.Captures;
		GhostLocation = move.GhostLocation;

		foreach (var capture in captures)
		{
			SetBoardPiece(capture.X, capture.Y, null);
			if (capture.Color == 'W') P2Captured.AddLast(capture);
			if (capture.Color == 'B') P1Captured.AddLast(capture);
			//board[capture.x][capture.y] = null;
		}

		foreach (var m in moves)
		{
			var p = board[m.From.X, m.From.Y];
			SetBoardPiece(m.From.X, m.From.Y, null);
			//board[m.from.x][m.from.y] = null;
			SetBoardPiece(m.To.X, m.To.Y, p);
			//board[m.to.x][m.to.y] = p;
			p?.ApplyMove(m.To);
		}

		var firstMove = moves.First().To;
		LastMoved = board[firstMove.X, firstMove.Y];
		if (LastMoved?.Name != "J")
		{
			LastCopyableMove = LastMoved;
		}

		return true;
	}

	public void SetBoardPiece(int x, int y, Piece? p)
	{
		board[x, y] = p;
		foreach (var d in dependents[x, y])
		{
			d.Dep.Invalidate(d.C);
		}
	}

	public void AddDep(Piece piece, int c, LinkedList<Point> l)
	{
		foreach (var p in l)
		{
			dependents[p.X, p.Y].AddLast(new Dependent(piece, c));
		}
	}

	public void GetDependencies()
	{
		for (int x = 0; x < SIZE; x++)
			for (int y = 0; y < SIZE; y++)
				board[x, y]?.GetValidMoves(false);
	}

	/*public boolean Move(Piece piece, Point loc){
		Board backup = null;
		if (!test) //implementing undo will get rid of this hack
			backup = [[copy.copy(self.board[i][j]) for j in range(8)] for i in range(8)]

		board[piece.x][piece.y] = null;
		board[loc.x][loc.y] = piece;
		piece.move(loc.x, loc.y);

		if (!test) {
			//if king is in check, undo
			if self.is_in_check(piece.color) {
				self.board = backup_board;
				return False;
			}
		}
		return true;
	}*/

	public bool InCheck(char color)
	{
		return InCheck(color, out var allChecks);
	}

	public bool InCheck(char color, out Dictionary<Piece, HashSet<Piece>> allChecks)
	{
		allChecks = new Dictionary<Piece, HashSet<Piece>>();
		foreach (var p in GetPieces(color))
		{
			if (!p.Royalty) continue;
			var dangers = p.InCheck();
			if (dangers.Count == 0) continue;
			allChecks.Add(p, dangers);
		}
		return allChecks.Count > 0;
	}

	public bool CanMove(char color)
	{
		foreach (var p in GetPieces(color))
		{
			if (p.GetValidMoves(true).Count > 0)
				return true;
		}
		return false;
	}

	// 0 nothing, 1 checkmate, 2 stalemate
	public int CheckMate()
	{
		if (CanMove(Turn)) return 0;
		return InCheck(Turn) ? 1 : 2;
	}

	//private bool CheckMate(char color) => !CanMove(color) && InCheck(color);

	//private bool StaleMate(char color) => !CanMove(color) && !InCheck(color);

	public void TestGame()
	{
		SetSize(8);
		//pieces =['Z', 'J', 'C', 'G', 'K', 'C', 'J', 'Z']
		board[0, 7] = new Piece('W', "Z", 500, this, 0, 7, Piece.GetWizardMoves());
		board[7, 7] = new Piece('W', "Z", 500, this, 7, 7, Piece.GetWizardMoves());
		board[1, 7] = new Piece('W', "J", 350, this, 1, 7, Piece.GetJokerMoves());
		board[6, 7] = new Piece('W', "J", 350, this, 6, 7, Piece.GetJokerMoves());
		board[2, 7] = new Piece('W', "C", 350, this, 2, 7, Piece.GetChampionMoves());
		board[5, 7] = new Piece('W', "C", 350, this, 5, 7, Piece.GetChampionMoves());
		board[3, 7] = new Piece('W', "G", 900, this, 3, 7, Piece.GetGryphonMoves(this));
		board[4, 7] = new Piece('W', "PR", 350, this, 4, 7, Piece.GetPrincessMoves(), true);

		board[0, 0] = new Piece('B', "Z", 500, this, 0, 0, Piece.GetWizardMoves());
		board[7, 0] = new Piece('B', "Z", 500, this, 7, 0, Piece.GetWizardMoves());
		board[1, 0] = new Piece('B', "J", 350, this, 1, 0, Piece.GetJokerMoves());
		board[6, 0] = new Piece('B', "J", 350, this, 6, 0, Piece.GetJokerMoves());
		board[2, 0] = new Piece('B', "C", 350, this, 2, 0, Piece.GetChampionMoves());
		board[5, 0] = new Piece('B', "C", 350, this, 5, 0, Piece.GetChampionMoves());
		board[3, 0] = new Piece('B', "G", 900, this, 3, 0, Piece.GetGryphonMoves(this));
		board[4, 0] = new Piece('B', "PR", 350, this, 4, 0, Piece.GetPrincessMoves(), true);

		for (int x = 0; x < 8; x++)
		{
			board[x, 6] = new Piece('W', "P", 100, this, x, 6, Piece.GetPawnMoves('W'));
			board[x, 6].Promotion = new Promotion(new Piece("G", 900, Piece.GetGryphonMoves(this)));

			board[x, 1] = new Piece('B', "P", 100, this, x, 1, Piece.GetPawnMoves('B'));
			board[x, 1].Promotion = new Promotion(new Piece("G", 900, Piece.GetGryphonMoves(this)));
		}

		Turn = 'W';
		DrawBoard();
	}

	public void NewGame()
	{
		SetSize(10);

		board[0, 8] = new Piece('W', "R", 500, this, 0, 8, Piece.GetRookMoves(this));
		board[9, 8] = new Piece('W', "R", 500, this, 9, 8, Piece.GetRookMoves(this));
		board[1, 8] = new Piece('W', "C", 350, this, 1, 8, Piece.GetChampionMoves());
		board[8, 8] = new Piece('W', "C", 350, this, 8, 8, Piece.GetChampionMoves());
		board[2, 8] = new Piece('W', "N", 350, this, 2, 8, Piece.GetKnightMoves());
		board[7, 8] = new Piece('W', "N", 350, this, 7, 8, Piece.GetKnightMoves());
		board[3, 8] = new Piece('W', "B", 350, this, 3, 8, Piece.GetBishopMoves(this));
		board[6, 8] = new Piece('W', "B", 350, this, 6, 8, Piece.GetBishopMoves(this));
		board[4, 8] = new Piece('W', "Q", 900, this, 4, 8, Piece.GetQueenMoves(this));
		board[5, 8] = new Piece('W', "K", 250, this, 5, 8, Piece.GetKingMoves(), true);

		board[0, 1] = new Piece('B', "R", 500, this, 0, 1, Piece.GetRookMoves(this));
		board[9, 1] = new Piece('B', "R", 500, this, 9, 1, Piece.GetRookMoves(this));
		board[1, 1] = new Piece('B', "C", 350, this, 1, 1, Piece.GetChampionMoves());
		board[8, 1] = new Piece('B', "C", 350, this, 8, 1, Piece.GetChampionMoves());
		board[2, 1] = new Piece('B', "N", 350, this, 2, 1, Piece.GetKnightMoves());
		board[7, 1] = new Piece('B', "N", 350, this, 7, 1, Piece.GetKnightMoves());
		board[3, 1] = new Piece('B', "B", 350, this, 3, 1, Piece.GetBishopMoves(this));
		board[6, 1] = new Piece('B', "B", 350, this, 6, 1, Piece.GetBishopMoves(this));
		board[4, 1] = new Piece('B', "Q", 900, this, 4, 1, Piece.GetQueenMoves(this));
		board[5, 1] = new Piece('B', "K", 250, this, 5, 1, Piece.GetKingMoves(), true);

		board[1, 9] = new Piece('W', "Z", 500, this, 1, 9, Piece.GetWizardMoves());
		board[8, 9] = new Piece('W', "Z", 500, this, 8, 9, Piece.GetWizardMoves());
		board[2, 9] = new Piece('W', "O", 1350, this, 2, 9, Piece.GetNightRiderMoves(this));
		board[7, 9] = new Piece('W', "O", 1350, this, 7, 9, Piece.GetNightRiderMoves(this));
		board[3, 9] = new Piece('W', "L", 1100, this, 3, 9, Piece.GetPaladinMoves(this));
		board[6, 9] = new Piece('W', "G", 750, this, 6, 9, Piece.GetGryphonMoves(this));
		board[5, 9] = new Piece('W', "E", 1900, this, 5, 9, Piece.GetBeastMoves());
		board[4, 9] = new Piece('W', "A", 1100, this, 4, 9, Piece.GetAmazonMoves(this));
		board[0, 9] = new Piece('W', "J", 900, this, 0, 9, Piece.GetJokerMoves());
		board[9, 9] = new Piece('W', "J", 900, this, 9, 9, Piece.GetJokerMoves());

		board[1, 0] = new Piece('B', "Z", 500, this, 1, 0, Piece.GetWizardMoves());
		board[8, 0] = new Piece('B', "Z", 500, this, 8, 0, Piece.GetWizardMoves());
		board[2, 0] = new Piece('B', "O", 1350, this, 2, 0, Piece.GetNightRiderMoves(this));
		board[7, 0] = new Piece('B', "O", 1350, this, 7, 0, Piece.GetNightRiderMoves(this));
		board[3, 0] = new Piece('B', "L", 1100, this, 3, 0, Piece.GetPaladinMoves(this));
		board[6, 0] = new Piece('B', "G", 750, this, 6, 0, Piece.GetGryphonMoves(this));
		board[5, 0] = new Piece('B', "E", 1900, this, 5, 0, Piece.GetBeastMoves());
		board[4, 0] = new Piece('B', "A", 1100, this, 4, 0, Piece.GetAmazonMoves(this));
		board[0, 0] = new Piece('B', "J", 900, this, 0, 0, Piece.GetJokerMoves());
		board[9, 0] = new Piece('B', "J", 900, this, 9, 0, Piece.GetJokerMoves());

		for (int x = 0; x < 10; x++)
		{
			board[x, 7] = new Piece('W', "P", 100, this, x, 7, Piece.GetPawnMoves('W'));
			board[x, 7].Promotion = new Promotion(new Piece("Q", 900, Piece.GetQueenMoves(this)));

			board[x, 2] = new Piece('B', "P", 100, this, x, 2, Piece.GetPawnMoves('B'));
			board[x, 2].Promotion = new Promotion(new Piece("Q", 900, Piece.GetQueenMoves(this)));
		}

		Turn = 'W';
		DrawBoard();
	}

	public void NewChessGame()
	{
		SetSize(8);

		board[0, 7] = new Piece('W', "R", 500, this, 0, 7, Piece.GetRookMoves(this));
		board[7, 7] = new Piece('W', "R", 500, this, 7, 7, Piece.GetRookMoves(this));
		board[1, 7] = new Piece('W', "N", 350, this, 1, 7, Piece.GetKnightMoves());
		board[6, 7] = new Piece('W', "N", 350, this, 6, 7, Piece.GetKnightMoves());
		board[2, 7] = new Piece('W', "B", 350, this, 2, 7, Piece.GetBishopMoves(this));
		board[5, 7] = new Piece('W', "B", 350, this, 5, 7, Piece.GetBishopMoves(this));
		board[3, 7] = new Piece('W', "Q", 900, this, 3, 7, Piece.GetQueenMoves(this));
		board[4, 7] = new Piece('W', "K", 350, this, 4, 7, Piece.GetKingMoves(), true);

		board[0, 0] = new Piece('B', "R", 500, this, 0, 0, Piece.GetRookMoves(this));
		board[7, 0] = new Piece('B', "R", 500, this, 7, 0, Piece.GetRookMoves(this));
		board[1, 0] = new Piece('B', "N", 350, this, 1, 0, Piece.GetKnightMoves());
		board[6, 0] = new Piece('B', "N", 350, this, 6, 0, Piece.GetKnightMoves());
		board[2, 0] = new Piece('B', "B", 350, this, 2, 0, Piece.GetBishopMoves(this));
		board[5, 0] = new Piece('B', "B", 350, this, 5, 0, Piece.GetBishopMoves(this));
		board[3, 0] = new Piece('B', "Q", 900, this, 3, 0, Piece.GetQueenMoves(this));
		board[4, 0] = new Piece('B', "K", 350, this, 4, 0, Piece.GetKingMoves(), true);

		for (int x = 0; x < 8; x++)
		{
			board[x, 6] = new Piece('W', "P", 100, this, x, 6, Piece.GetPawnMoves('W'));
			board[x, 6].Promotion = new Promotion(new Piece("Q", 900, Piece.GetQueenMoves(this)));

			board[x, 1] = new Piece('B', "P", 100, this, x, 1, Piece.GetPawnMoves('B'));
			board[x, 1].Promotion = new Promotion(new Piece("Q", 900, Piece.GetQueenMoves(this)));
		}

		Turn = 'W';
		DrawBoard();
	}

	public void NewCheckersGame()
	{
		SetSize(8);
		for (int i = 0; i < 12; i++)
		{
			int y2 = i / 4;
			int y1 = SIZE - y2 - 1;
			int x1 = (i % 4) * 2 + ((y1 % 2 == 0) ? 0 : 1);
			int x2 = SIZE - x1 - 1;

			board[x1, y1] = new Piece('W', "P", 100, this, x1, y1, Piece.GetCheckerMoves('W'));
			board[x2, y2] = new Piece('B', "P", 100, this, x2, y2, Piece.GetCheckerMoves('B'));
		}
		Turn = 'W';
		DrawBoard();
	}

	public LinkedList<Piece> GetPieces(char color)
	{
		//TODO make player class and keep list of pieces
		var pieces = new LinkedList<Piece>();
		for (int x = 0; x < SIZE; x++)
		{
			for (int y = 0; y < SIZE; y++)
			{
				if (board[x, y] != null && board[x, y].Color == color)
				{
					pieces.AddLast(board[x, y]);
				}
			}
		}
		return pieces;
	}

	public void NextTurn()
	{
		if (Turn == 'W')
		{
			Turn = 'B';
			/*if(!AIOn) return;
            PlayerMove AIMove = AI.makeMove(this, 'B', 2);
            if(AIMove!= null)//shouldn't happen
                Move(AIMove);
            drawBoard();
            turn = 'W';*/
		}
		else
		{
			Turn = 'W';
		}

		if (GetPieces(Turn).Count == 0)
		{
			NextTurn();
		}
	}

	public void LookForCheck()
	{
		CheckPieces.Clear();
		InCheck(Turn, out var allChecks);
		foreach(var check in allChecks)
		{
			CheckPieces.Add(check.Key);
			CheckPieces.AddRange(check.Value);
		}
	}

	public void Ready()
	{
		if (!AIOn || Turn != 'B') return;

		GetDependencies();
		var aiMove = AI.MakeMove(this, 'B', AILEVEL);
		if (aiMove != null) //no valid moves, ei checkmate
			Move(aiMove);
		Turn = 'W';
		LookForCheck();
		DrawBoard();
	}


	//returns how far off the board the point is;
	public int OnBoard(Point p) => OnBoard(p.X, p.Y);

	public int OnBoard(int x, int y)
	{
		int off = OnBoardX(x);
		if (off != 0) return off;
		return OnBoardY(y);
	}

	public int OnBoardX(int x)
	{
		if (x < 0) return -x;
		if (x >= SIZE) return x - SIZE + 1;
		return 0;
	}

	public int OnBoardY(int y)
	{
		if (y < 0) return -y;
		if (y >= SIZE) return y - SIZE + 1;
		return 0;
	}

	//deep copy
	public Board CopyBoard()
	{
		//TODO should lastMoved and ghostLocation be copied as well? Is this why AI sometimes is dumb?
		Board copy = new Board();
		copy.SetSize(SIZE);
		for (int i = 0; i < SIZE; i++)
		{
			for (int j = 0; j < SIZE; j++)
			{
				if (board[i, j] != null)
				{
					copy.board[i, j] = new Piece(board[i, j], copy);
				}
			}
		}
		return copy;
	}

	public Board TestMove(PlayerMove m)
	{
		Board testBoard = CopyBoard();
		testBoard.Move(m);
		return testBoard;
	}

	public void DrawBoard()
	{
		ui?.InvalidateVisual();
	}

	public void PrintBoard()
	{
		for (int i = 0; i < SIZE; i++)
		{
			for (int j = 0; j < SIZE; j++)
			{
				if (board[j, i] == null)
					Console.Write("-- ");
				else
					Console.Write($"{board[j, i]} ");
			}
			Console.WriteLine();
		}
	}

	public static char OtherColor(char color) => color == 'B' ? 'W' : 'B';

	public static Point GetForwardDirection(char color) => new Point(0, color == 'B' ? 1 : -1);
}