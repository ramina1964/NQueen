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
            var columnIndex = position.ColumnIndex;
            var rowIndex = position.RowIndex;

            if (rowIndex < 0 || columnIndex < 0)
                continue;

            var square = Squares.FirstOrDefault(sq =>
                sq.Position.ColumnIndex == columnIndex && sq.Position.RowIndex == rowIndex);

            if (square is null)
                Debug.WriteLine($"PlaceQueens: no square at ({columnIndex}, {rowIndex}).");
            else
                square.ImagePath = QueenImagePath;
        }
    }

    public void CreateSquares(int boardSize)
    {
        if (IsBoardStateUpdatedAndSquaresPopulated(boardSize))
            return;

        Squares.Clear();
        var cellSize = WindowWidth / boardSize;

        for (var rowIndex = boardSize - 1; rowIndex >= 0; rowIndex--)
        {
            for (var columnIndex = 0; columnIndex < boardSize; columnIndex++)
            {
                var position = new Position(columnIndex, rowIndex);
                Squares.Add(new SquareViewModel(position, FindColor(position))
                {
                    ImagePath = string.Empty,
                    Height = cellSize,
                    Width = cellSize,
                });
            }
        }

        _lastBoardSize = boardSize;
        _lastWidth = WindowWidth;
        _lastHeight = WindowHeight;
    }

    public bool IsBoardStateUpdatedAndSquaresPopulated(int boardSize) =>
        boardSize > 0 &&
        boardSize == _lastBoardSize &&
        WindowWidth == _lastWidth &&
        WindowHeight == _lastHeight &&
        Squares.Count > 0;

    public void ClearImages()
    {
        foreach (var sq in Squares)
            sq.ImagePath = null!;
    }

    // --- Private ---

    private static SolidColorBrush FindColor(Position position) =>
        (position.ColumnIndex + position.RowIndex) % 2 == 1
            ? s_wheatBrush
            : s_brownBrush;

    private static readonly SolidColorBrush s_wheatBrush = new(Colors.Wheat);
    private static readonly SolidColorBrush s_brownBrush = new(Colors.Brown);

    private int _lastBoardSize = -1;
    private double _lastWidth = -1;
    private double _lastHeight = -1;

    // Stored for future dispatcher use; suppress unused-field warning
    private readonly IDispatcher _uiDispatcher = uiDispatcher
        ?? throw new ArgumentNullException(nameof(uiDispatcher));
}
