using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using Avalon.Game;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalon.Game.Board;
using Point = Avalon.Game.Point;

namespace Avalon.Controls;

public partial class ChessControl : Control
{
	public Board board;
	private bool settings = false;
	private List<ButtonArea> buttons = new();
	private readonly IPen strokePen = new Pen(Brushes.Black, 4);

	private const int GAP = 10;
	private const double PIECE_GAP = 0.1;
	private readonly Color DARK_COLOR = Color.FromRgb(150, 100, 50);
	private readonly Color LIGHT_COLOR = Color.FromRgb(250, 200, 100);
	private readonly Color VALID_COLOR = Color.FromRgb(50, 200, 200);
	private readonly Color LAST_COLOR = Color.FromRgb(200, 50, 50);
	private readonly Color[] TEST_COLOR = new[]
	{
		Color.FromRgb(200, 50, 50),
		Color.FromRgb(50, 50, 200),
		Color.FromRgb(50, 200, 50),
		Color.FromRgb(200, 50, 200),
		Color.FromRgb(225, 225, 75),
		Color.FromRgb(50, 200, 200),
		Color.FromRgb(200, 50, 125),
		Color.FromRgb(50, 200, 125)
	};
	private const double VALID_GAP = 0.15;

	private int squares;
	private int width, height, minx, miny, size, squareSize;

	private class ButtonArea
	{
		public int X, Y, W, H;
		public string Event;

		public ButtonArea(int x, int y, int w, int h, string evt)
		{
			X = x;
			Y = y;
			W = w;
			H = h;
			Event = evt;
		}
	}

	public ChessControl()
	{
		PointerPressed += OnPointerPressed;
		board = Board.SetUI(this);
		ChangeBoardSize(board.SIZE);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		width = (int)(finalSize.Width - 2 * GAP);
		height = (int)(finalSize.Height - 2 * GAP);
		size = Math.Min(width, height);
		minx = width / 2 - size / 2 + GAP;
		miny = height / 2 - size / 2 + GAP;
		squareSize = size / squares;

		return base.ArrangeOverride(finalSize);
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		var point = e.GetPosition(this);
		int a = (int)((point.X - minx) / squareSize);
        int b = (int)((point.Y - miny) / squareSize);
        if(a < 0 || a >= squares) return;
        board.Touch(a, b);
	}

	public void ChangeBoardSize(int s)
	{
		squares = s;
		squareSize = size / squares;
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);
		DrawBoard(context);
		DrawCaptured(context);
		//DrawSettings(context);

		Dispatcher.UIThread.InvokeAsync(() => board.Ready(), DispatcherPriority.Background);
	}

	private void DrawSettings(DrawingContext context)
	{
		buttons.Clear();
		// TODO: Draw gear and settings menu
	}

	private void DrawButton(DrawingContext context, int x, int y, string text, int s)
	{
		// TODO: Implement drawing button with text
	}

	private void DrawButton2(DrawingContext context, int x, int y, int w, int h, string text)
	{
		DrawButton2(context, x, y, w, h, text, Colors.White);
	}

	private void DrawButton2(DrawingContext context, int x, int y, int w, int h, string text, Color color)
	{
		buttons.Add(new ButtonArea(x, y, w, h, text));
		// TODO: Draw button with outline and centered text
	}

	private Rect FindRightTextSize(Rect bounds, int l, int u, string text)
	{
		// TODO: This requires text measuring in Avalonia.
		// Stubbed for now
		return bounds;
	}

	private void DrawBoard(DrawingContext context)
	{
		// Squares
		for (int i = 0; i < squares; i++)
		{
			for (int j = 0; j < squares; j++)
			{
				DrawSquare(context, BackgroundColor(i, j), Translate(i, j));
			}
		}

		// Last Moved
		if (board.LastMoved != null)
		{
			Point lastP = board.LastMoved.Location();
			DrawHighlightedSquare(context, LAST_COLOR, lastP);
		}

		// Selected Piece
		if (board.SelectedPiece != null)
		{
			Point selectedP = board.SelectedPiece.Location();
			DrawHighlightedSquare(context, TEST_COLOR[2], selectedP);
		}

		// Valid Moves
		foreach (var m in board.ValidMoves)
		{
			Point p = m.Moves.First().To;
			var color = VALID_COLOR;
			if (m.Test != -1) color = TEST_COLOR[m.Test];
			DrawHighlightedSquare(context, color, p);
		}

		// Pieces
		for (int i = 0; i < squares; i++)
		{
			for (int j = 0; j < squares; j++)
			{
				var p = board.GetPiece(i, j);
				if (p == null) continue;

				// Optional: Uncomment to highlight pieces in check
				// if (p.InCheck()) DrawHighlightedSquare(drawingContext, TEST_COLOR[2], new Point(i, j));

				DrawPiece(
					context,
					p.Image,
					minx + size * p.X / squares + squareSize * PIECE_GAP,
					miny + size * p.Y / squares + squareSize * PIECE_GAP,
					squareSize * (1 - PIECE_GAP * 2)
				);
			}
		}

		// Check pieces
		foreach (var p in board.CheckPieces)
		{
			var loc = Translate(new Point(p.X, p.Y));
			var destRect = new Rect(loc.X + squareSize * 0.5, loc.Y - squareSize * 0.2, squareSize * 0.7, squareSize * 0.7);
			context.DrawImage(CheckMark, destRect);
		}

		// Checkmate
		var checkMateState = board.CheckMate();
		if (checkMateState > 0)
		{
			var formattedText = new FormattedText(
				checkMateState == 1 ? "Checkmate!" : "Stalemate!",
				CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
				70, // font size
				Brushes.Red // foreground color
			);

			var rect = new Rect(0, 0, Bounds.Width-350, Bounds.Height);

			context.DrawText(formattedText, rect.Center);
		}
	}

	IImage CheckMark = new Bitmap(AssetLoader.Open(new Uri($"avares://Avalon/Assets/!.png")));

	private void DrawCaptured(DrawingContext context)
	{
		int i = 0;
		double spacing = squareSize * PIECE_GAP;

		foreach (var p in board.P2Captured)
		{
			var po = board.CapturedIntToPosition('B', i);
			DrawPiece(
				context,
				p.Image,
				minx + po.X * squareSize + spacing,
				miny + po.Y * squareSize + spacing,
				squareSize * (1 - PIECE_GAP * 2)
			);
			i++;
		}

		i = 0;
		foreach (var p in board.P1Captured)
		{
			var po = board.CapturedIntToPosition('W', i);
			DrawPiece(
				context,
				p.Image,
				minx + po.X * squareSize + spacing,
				miny + po.Y * squareSize + spacing,
				squareSize * (1 - PIECE_GAP * 2)
			);
			i++;
		}
	}

	private void DrawHighlightedSquare(DrawingContext context, Color color, Point p)
	{
		DrawSquare(context, color, Translate(p));
		Point gap = new Point((int)(squareSize * VALID_GAP), (int)(squareSize * VALID_GAP));
		DrawSquare(context, BackgroundColor(p), Point.Add(Translate(p), gap), false, (int)(squareSize * (1 - VALID_GAP * 2)));
	}

	private Color BackgroundColor(int x, int y) => (x + y) % 2 == 0 ? LIGHT_COLOR : DARK_COLOR;

	private Color BackgroundColor(Point p) => BackgroundColor(p.X, p.Y);

	private void DrawPiece(DrawingContext context, IImage piece, double x, double y, double size)
	{
		DrawPiece(context, piece, (int)x, (int)y, (int)size);
	}

	private void DrawPiece(DrawingContext context, IImage piece, int x, int y, int size)
	{
		if (piece == null)
		{
			Console.WriteLine("Image not found");
			return;
		}
		var destRect = new Rect(x, y, size, size);
		context.DrawImage(piece, destRect);
	}
	private void DrawSquare(DrawingContext context, Color color, Point p)
	{
		DrawSquare(context, color, p, true, squareSize);
	}

	private void DrawSquare(DrawingContext context, Color color, Point p, bool border, int squareSize)
	{
		var rect = new Rect(p.X, p.Y, squareSize, squareSize);
		context.FillRectangle(new SolidColorBrush(color), rect);
		if (border)
		{
			context.DrawRectangle(null, strokePen, rect);
		}
	}

	private Point Translate(int x, int y) => new Point(minx + size * x / squares, miny + size * y / squares);

	private Point Translate(Point p) => Translate(p.X, p.Y);

	private long lastClickTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
		if (time - lastClickTime < 150) return;
		lastClickTime = time;

		var point = e.GetPosition(this);
		HandleClickAt((int)point.X, (int)point.Y);
	}

	private void HandleClickAt(int x, int y)
	{
		foreach (var b in buttons)
		{
			if (x >= b.X && x <= b.X + b.W && y >= b.Y && y <= b.Y + b.H)
			{
				HandleClick(b.Event);
				return;
			}
		}

		int a = (x - minx) / squareSize;
		int bVal = (y - miny) / squareSize;

		if (a < 0 || a >= squares) return;

		board.Touch(a, bVal);
	}

	private void HandleClick(string evt)
	{
		if (evt == "gear")
		{
			ToggleSettings();
		}
		else if (evt == "Singleplayer")
		{
			SetMode(true);
			HideSettings();
		}
		else if (evt == "Multiplayer")
		{
			SetMode(false);
			HideSettings();
		}
		else if (evt == "Checkers" || evt == "Chess" || evt == "Fantasy Chess" || evt == "Test")
		{
			//SetMode(evt);
			HideSettings();
		}
	}

	private void ToggleSettings()
	{
		settings = !settings;
		board.DrawBoard();
	}

	public void SetMode(bool ai)
	{
		if (board.AIOn == ai) return;
		board = Board.StartNewGame(ai);
	}

	public void SetMode(GameMode mode)
	{
		if (board.Mode == mode) return;
		board = StartNewGame(mode);
	}

	public void SetMode(GameMode mode, bool ai)
	{
		board = StartNewGame(ai, mode);
	}

	private void HideSettings()
	{
		settings = false;
		board.DrawBoard();
	}
}