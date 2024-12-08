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

    public double SquareSize => Math.Min(WindowWidth, WindowHeight) / BoardSize;

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

    public void CreateSquares(int boardSize, IEnumerable<SquareViewModel> squares)
    {
        BoardSize = boardSize;
        var sqList = squares.ToList();
        for (var i = 0; i < boardSize; i++)
        {
            for (var j = 0; j < boardSize; j++)
            {
                var pos = new Position(i, j);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = null,
                    Height = SquareSize,
                    Width = SquareSize,
                };

                sqList.Add(square);
            }
        }

        Squares.Clear();
        foreach (var square in sqList.OrderByDescending(sq => sq.Position.ColumnNo).ThenBy(sq => sq.Position.RowNo))
        {
            Squares.Add(square);
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
