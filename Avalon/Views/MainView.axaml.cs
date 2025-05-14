using Avalon.Controls;
using Avalon.Game;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace Avalon.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

		GameModeSelect.ItemsSource = new List<ItemsTuple>
		{
			new ItemsTuple("Classic Chess", Board.GameMode.Chess),
			new ItemsTuple("Checkers", Board.GameMode.Checkers),
			new ItemsTuple("Fantasy Chess Small", Board.GameMode.FantasySmall),
			new ItemsTuple("Fantasy Chess Large", Board.GameMode.FantasyLarge)
		};
		GameModeSelect.SelectedIndex = 2;

		AIModeSelect.ItemsSource = new List<ItemsTuple>
		{
			new ItemsTuple("AI On", true),
			new ItemsTuple("AI Off", false)
		};
		AIModeSelect.SelectedIndex = 0;
	}

	private void OnRestartClicked(object? sender, RoutedEventArgs e)
	{
		if (!(GameModeSelect.SelectedItem is ItemsTuple modeOption)) return;
		if (!(AIModeSelect.SelectedItem is ItemsTuple aiOption)) return;

		chessControl.SetMode((Board.GameMode)modeOption.Value, (bool)aiOption.Value);
	}

	public class ItemsTuple
	{
		public string Label { get; }
		public object Value { get; }

		public ItemsTuple(string label, object value)
		{
			Label = label;
			Value = value;
		}

		public override string ToString() => Label;
	}
}