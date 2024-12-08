namespace NQueen.GUI.Views;

public class ChessboardGrid : Grid
{
    public ChessboardGrid(byte size)
    {
        Size = size;
        CreateGrid();
    }

    public byte Size { get; set; }

    public int WidthBorder => (int)(ActualWidth / Size);
    public int HeightBorder => (int)(ActualHeight / Size);

    // Test of dynamic grid written in code - no Xaml
    public void CreateGrid()
    {
        ColumnDefinitions.Clear();
        RowDefinitions.Clear();
        Children.Clear();

        for (byte i = 0; i < Size; i++)
        {
            ColumnDefinitions.Add(new ColumnDefinition());
            RowDefinitions.Add(new RowDefinition());
        }

        for (byte i = 0; i < Size; i++)
        {
            for (byte j = 0; j < Size; j++)
            {
                SolidColorBrush color = new((i + j) % 2 == 0 ? Colors.Wheat : Colors.Brown);
                Position pos = new(i, j);
                SquareViewModel sq = new(pos, color);
                Border border = new()
                {
                    Background = color,
                    DataContext = sq
                };

                SetColumn(border, j);
                SetRow(border, i);
                Children.Add(border);
            }
        }

        SizeChanged += HandleBoardSizeChanged;
    }

    private void HandleBoardSizeChanged(object sender, SizeChangedEventArgs e)
    {
        foreach (Border border in Children)
        {
            border.Width = WidthBorder;
            border.Height = HeightBorder;
        }
    }
}
