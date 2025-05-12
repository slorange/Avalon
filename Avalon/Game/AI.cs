using System.Collections.Generic;

namespace Avalon.Game;

public static class AI
{
	// TODO: Implement alpha-beta pruning
	// example depth 2, p1 does move A, p2 moves, even game,
	//                  p1 does move B, p2 wins a rook, quit early, because we know p1 has a better move

	// TODO: Adapt AI level based on time instead of fixed depth

	public static PlayerMove? MakeMove(Board board, char color, int skill)
	{
		var mine = board.GetPieces(color);
		var validMoves = new LinkedList<PlayerMove>();

		foreach (var piece in mine)
		{
			var moves = piece.GetValidMoves(false);
			foreach (var move in moves)
			{
				var testBoard = board.CopyBoard(); //TODO use Board.testMove()
				testBoard.Move(move);

				if (testBoard.InCheck(color)) continue; //can't move into check

				validMoves.AddLast(move);
				bool foundCounterMove = false;

				if (skill > 1)
				{
					var counterMove = MakeMove(testBoard, Board.OtherColor(color), skill - 1);
					if (counterMove != null) //TODO stalemate??
					{
						move.Score = -counterMove.Score;
						foundCounterMove = true;
					}
				}

				if (!foundCounterMove)
				{
					move.Score = Eval(testBoard, color);
				}
			}
		}

		PlayerMove? bestMove = null;
		foreach (var move in validMoves)
		{
			if (bestMove == null || move.Score > bestMove.Score)
			{
				bestMove = move;
			}
		}

		return bestMove;
	}

	public static int Eval(Board board, char color)
	{
		var mine = board.GetPieces(color);
		var his = board.GetPieces(Board.OtherColor(color));
		int score = 0;

		foreach (var piece in mine)
		{
			score += piece.GetValue();
		}

		foreach (var piece in his)
		{
			score -= piece.GetValue();
		}

		return score;
	}
}