namespace NQueen.GUI.Views;

public class ChessboardGrid(int size) : Grid
{
    public static int WindowHeight => 500;

    public static int WindowWidth => 500;

    public int Size { get; set; } = size;

    public int Column { get; set; }

    public int Row { get; set; }

    public int WidthBorder => (WindowWidth - 50) / Size;

    public int HeightBorder => (WindowHeight - 50) / Size;

    // Test of dynamic grid written in code - no Xaml
    public void CreateGrid()
    {
        GridLength width = new(WidthBorder);
        GridLength height = new(HeightBorder);
        Grid grid = new() { Height = WindowHeight, Width = WindowHeight };
        for (var i = 0; i < Size; i++)
        {
            ColumnDefinition column = new() { Width = width, Tag = i };
            RowDefinition row = new() { Height = height, Tag = i };
            grid.ColumnDefinitions.Add(column);
            grid.RowDefinitions.Add(row);
            for (var j = 0; j < Size; j++)
            {
                SolidColorBrush color = new(Colors.Wheat);
                Position pos = new(i, j);
                SquareViewModel sq = new(pos, color);
                Border border = new()
                {
                    Background = color,
                    Height = HeightBorder,
                    Width = WidthBorder,
                    DataContext = sq
                };

                SetColumn(border, j);
                SetRow(border, i);
            }
        }
    }
}
