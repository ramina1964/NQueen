namespace NQueen.GUI.ViewModels;

public partial class ChessboardViewModel(IDispatcher uiDispatcher) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<SquareViewModel> _squares = [];

    [ObservableProperty]
    private double _windowWidth;

    [ObservableProperty]
    private double _windowHeight;

    public string QueenImagePath { get; } = BoardSettings.QueenImageResource;

    public void PlaceQueens(IEnumerable<Position> positions)
    {
        ClearImages();

        foreach (var pos in positions)
        {
            int rowIndex = pos.RowNo;
            int colIndex = pos.ColumnNo;

            // Todo: Fix the issue of invalid positions and then remove the following defensive code.
            if (rowIndex < 0 || colIndex < 0)
                continue;

            try
            {
                var square = Squares.First(sq => sq.Position.RowNo == rowIndex &&
                                                 sq.Position.ColumnNo == colIndex);
                square.ImagePath = QueenImagePath;
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"Error in PlaceQueens: No matching square found for position ({rowIndex}, {colIndex}). Exception: {ex.Message}");
            }
        }
    }

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

    // --- Private Methods and Fields ---

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

    private int _lastBoardSize = -1;
    private double _lastWidth = -1;
    private double _lastHeight = -1;

    private readonly IDispatcher _uiDispatcher = uiDispatcher
        ?? throw new ArgumentNullException(nameof(_uiDispatcher));
}
