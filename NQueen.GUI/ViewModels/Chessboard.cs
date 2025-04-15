namespace NQueen.GUI.ViewModels;

// Todo: Throws exception in PlaceQueens(), when DisplayMode is turned on, see below:
// System.InvalidOperationException: 'Sequence contains no matching element.
public class Chessboard : ObservableObject
{
    public Chessboard(IDispatcher uiDispatcher)
    {
        _uiDispatcher = uiDispatcher
            ?? throw new ArgumentNullException(nameof(uiDispatcher));

        Squares = [];
        QueenImagePath = @"..\..\Images\WhiteQueen.png";
    }

    public string QueenImagePath { get; }

    public ObservableCollection<SquareViewModel> Squares { get; set; }

    public double WindowWidth { get; set; }

    public double WindowHeight { get; set; }

    public void PlaceQueens(IEnumerable<Position> positions)
    {
        // Clear board
        ClearImages();

        // Place queens
        foreach (var pos in positions)
        {
            try
            {
                var square = Squares.First(sq => pos.RowNo == sq.Position.RowNo &&
                                                 pos.ColumnNo == sq.Position.ColumnNo);
                square.ImagePath = QueenImagePath;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Error in PlaceQueens: No matching square found for position ({pos.RowNo}, {pos.ColumnNo}). Exception: {ex.Message}");
            }
        }
    }

    public void CreateSquares(int boardSize, IEnumerable<SquareViewModel> squares)
    {
        var width = (int)WindowWidth / boardSize;
        var height = width;

        var sqList = squares.ToList();
        for (var i = 0; i < boardSize; i++)
        {
            for (var j = 0; j < boardSize; j++)
            {
                var pos = new Position(i, j);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = null,
                    Height = height,
                    Width = width,
                };

                sqList.Add(square);
            }
        }

        sqList
            .OrderByDescending(sq => sq.Position.ColumnNo)
            .ThenBy(sq => sq.Position.RowNo).ToList()
            .ForEach(sq => Squares.Add(sq));
    }

    private void ClearImages() =>
        Squares
            .ToList()
            .ForEach(sq => sq.ImagePath = null);

    private static SolidColorBrush FindColor(Position pos)
    {
        var col = (pos.RowNo + pos.ColumnNo) % 2 == 1
            ? Colors.Wheat
            : Colors.Brown;

        return new SolidColorBrush(col);
    }

    private readonly IDispatcher _uiDispatcher;
}
