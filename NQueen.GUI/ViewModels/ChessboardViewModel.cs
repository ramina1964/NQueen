namespace NQueen.GUI.ViewModels;

public partial class ChessboardViewModel(IDispatcher uiDispatcher) : ObservableObject
{
    public void PlaceQueens(IEnumerable<Position> positions)
    {
        ClearImages();

        foreach (var pos in positions)
        {
            int row = pos.RowNo;
            int col = pos.ColumnNo;

            try
            {
                var square = Squares.First(sq => sq.Position.RowNo == row &&
                                                 sq.Position.ColumnNo == col);
                square.ImagePath = QueenImagePath;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Error in PlaceQueens: No matching square found for position ({row}, {col}). Exception: {ex.Message}");
            }
        }
    }

    public string QueenImagePath { get; } = BoardSettings.QueenImageResource;

    [ObservableProperty]
    private ObservableCollection<SquareViewModel> _squares = [];

    [ObservableProperty]
    private double _windowWidth;

    [ObservableProperty]
    private double _windowHeight;

    private int _lastBoardSize = -1;
    private double _lastWidth = -1;
    private double _lastHeight = -1;

    public void CreateSquares(int boardSize)
    {
        if (IsBoardStateUpdatedAndSquaresPopulated(boardSize))
            return;

        Squares.Clear();
        var width = WindowWidth / boardSize;
        var height = width;

        // Fill columns left-to-right, and in each column from bottom (row 0) to top (row N-1)
        for (var col = 0; col < boardSize; col++)
        {
            for (var row = 0; row < boardSize; row++)
            {
                // No inversion: row 0 is bottom, col 0 is left
                var pos = new Position(row, col);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = string.Empty,
                    Height = height,
                    Width = width,
                };

                Squares.Add(square);
            }
        }

        _lastBoardSize = boardSize;
        _lastWidth = WindowWidth;
        _lastHeight = WindowHeight;
    }

    // This method is used as a condition for early termination of CreateSquares()
    private bool IsBoardStateUpdatedAndSquaresPopulated(int boardSize) =>
        boardSize > 0 &&
        boardSize == _lastBoardSize &&
        WindowWidth == _lastWidth &&
        WindowHeight == _lastHeight &&
        Squares.Count > 0;

    private void ClearImages()
    {
        foreach (var sq in Squares)
            sq.ImagePath = null!;
    }

    private static SolidColorBrush FindColor(Position pos)
    {
        var col = (pos.RowNo + pos.ColumnNo) % 2 == 1
            ? Colors.Wheat
            : Colors.Brown;

        return new SolidColorBrush(col);
    }

    private readonly IDispatcher _uiDispatcher = uiDispatcher
        ?? throw new ArgumentNullException(nameof(_uiDispatcher));
}
