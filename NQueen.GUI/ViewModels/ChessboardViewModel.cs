namespace NQueen.GUI.ViewModels;

public class ChessboardViewModel : ObservableObject
{
    public ChessboardViewModel()
    {
        Squares = [];
        QueenImagePath = Constants.QueenImagePath;
    }

    public string QueenImagePath { get; }

    public ObservableCollection<SquareViewModel> Squares { get; set; }

    public double WindowWidth { get; set; }

    public double WindowHeight { get; set; }

    public double SquareSize =>
        BoardSize > 0
        ? Math.Min(WindowWidth, WindowHeight) / BoardSize
        : 0;

    public int BoardSize { get; set; }

    public void PlaceQueens(IEnumerable<Position> positions)
    {
        ClearImages();

        foreach (var pos in positions)
        {
            var square = Squares.FirstOrDefault(sq => pos.RowNo == sq.Position.RowNo
                && pos.ColumnNo == sq.Position.ColumnNo);
            if (square != null)
            {
                square.ImagePath = QueenImagePath;
            }
        }
    }

    public void InitializeSquares(int boardSize)
    {
        BoardSize = boardSize;
        var squareSize = SquareSize;

        Squares.Clear();
        for (var col = boardSize - 1; col >= 0; col--)
        {
            for (var row = 0; row < boardSize; row++)
            {
                var pos = new Position(row, col);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = null,
                    Height = squareSize,
                    Width = squareSize,
                };

                Squares.Add(square);
            }
        }
    }

    private void ClearImages() =>
        Squares.ToList().ForEach(sq => sq.ImagePath = null);

    private static SolidColorBrush FindColor(Position pos)
    {
        var col = (pos.RowNo + pos.ColumnNo) % 2 == 1 ? Colors.Wheat : Colors.Brown;
        return new SolidColorBrush(col);
    }
}


