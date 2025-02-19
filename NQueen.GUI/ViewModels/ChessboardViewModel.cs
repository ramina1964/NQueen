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
            }
        }
    }


    public void CreateSquares(byte boardSize, IEnumerable<SquareViewModel> squares)
    {
        var width = WindowWidth / boardSize;
        var height = WindowHeight / boardSize;

        var sqList = squares.ToList();
        for (byte i = 0; i < boardSize; i++)
        {
            for (byte j = 0; j < boardSize; j++)
            {
                var pos = new Position(i, j);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = null,
                    Height = height,
                    Width = width,
                    IsOffscreen = true
                };

                sqList.Add(square);
            }
        }

        sqList
            .OrderByDescending(sq => sq.Position.ColumnNo)
            .ThenBy(sq => sq.Position.RowNo).ToList()
            .ForEach(sq => Squares.Add(sq));
    }

    private void ClearImages()
    {
        Squares
            .ToList()
            .ForEach(sq =>
            {
                sq.ImagePath = null;
                sq.IsOffscreen = true;
            });
    }

    private static SolidColorBrush FindColor(Position pos)
    {
        var col = (pos.RowNo + pos.ColumnNo) % 2 == 1
            ? Colors.Wheat
            : Colors.Brown;

        return new SolidColorBrush(col);
    }
}
