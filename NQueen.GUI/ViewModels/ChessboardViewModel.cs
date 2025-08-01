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

        foreach (var position in positions)
        {
            int colIndex = position.ColumnNo;
            int rowIndex = position.RowIndex;

            if (rowIndex < 0 || colIndex < 0)
                continue;

            try
            {

                var square = Squares.First(sq =>
                    sq.Position.ColumnNo == colIndex && sq.Position.RowIndex == rowIndex);
                
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

        for (var colIndex = 0; colIndex < boardSize; colIndex++)
        {
            for (var rowIndex = 0; rowIndex < boardSize; rowIndex++)
            {
                var position = new Position(colIndex, rowIndex);
                var square = new SquareViewModel(position, FindColor(position))
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

    private static SolidColorBrush FindColor(Position position)
    {
        var colIndex = (position.ColumnNo + position.RowIndex) % 2 == 1
            ? Colors.Wheat
            : Colors.Brown;

        return new SolidColorBrush(colIndex);
    }

    private int _lastBoardSize = -1;
    private double _lastWidth = -1;
    private double _lastHeight = -1;

    private readonly IDispatcher _uiDispatcher = uiDispatcher
        ?? throw new ArgumentNullException(nameof(_uiDispatcher));
}
