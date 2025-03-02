namespace NQueen.GUI.ViewModels;

public class ChessboardViewModel : ObservableObject
{
    public ChessboardViewModel()
    {
        Squares = [];
        QueenImagePath = Constants.QueenImagePath;
    }

    public string QueenImagePath { get; }

    public ObservableCollection<SquareViewModel> Squares { get; set; } = new();

    public double WindowWidth { get; set; }

    public double WindowHeight { get; set; }

    public void PlaceQueens(IEnumerable<Position> positions)
    {
        if (positions == null) return;

        // Clear board
        ClearImages();

        // Place queens
        foreach (var pos in positions)
        {
            var square = Squares.FirstOrDefault(sq => pos.RowNo == sq.Position.RowNo
                && pos.ColumnNo == sq.Position.ColumnNo);

            if (square != null)
            {
                square.ImagePath = QueenImagePath;
                square.IsOffscreen = false;
                square.BoundingRectangle = new Rect(
                    square.Position.ColumnNo * square.Width,
                    square.Position.RowNo * square.Height,
                    square.Width,
                    square.Height);
            }
        }
    }

    public void CreateSquares(byte boardSize, IEnumerable<SquareViewModel> squares)
    {
        if (boardSize < BoardSettings.MinBoardSize || boardSize > BoardSettings.ByteMaxValue)
        {
            // Handle invalid board size
            return;
        }

        var width = WindowWidth / boardSize;
        var height = WindowHeight / boardSize;

        var sqList = squares.ToList();
        for (byte i = 0; i < boardSize; i++)
        {
            for (byte j = 0; j < boardSize; j++)
            {
                var pos = new Position(i, j);
                var square = new SquareViewModel(pos, FindColor(pos), width, height)
                {
                    ImagePath = null,
                    IsOffscreen = true,
                    BoundingRectangle = new Rect(j * width, i * height, width, height)
                };

                sqList.Add(square);
            }
        }

        Squares = new ObservableCollection<SquareViewModel>(sqList
            .OrderByDescending(sq => sq.Position.ColumnNo)
            .ThenBy(sq => sq.Position.RowNo).ToList());
    }

    private void ClearImages()
    {
        foreach (var sq in Squares)
        {
            sq.ImagePath = null;
            sq.IsOffscreen = true;
            sq.BoundingRectangle = new Rect(
                sq.Position.ColumnNo * sq.Width,
                sq.Position.RowNo * sq.Height,
                sq.Width,
                sq.Height);
        }
    }

    private static SolidColorBrush FindColor(Position pos)
    {
        var col = (pos.RowNo + pos.ColumnNo) % 2 == 1
            ? Colors.Wheat
            : Colors.Brown;

        return new SolidColorBrush(col);
    }
}
