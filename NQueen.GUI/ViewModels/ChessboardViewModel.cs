namespace NQueen.GUI.ViewModels;

public partial class ChessboardViewModel(IDispatcher uiDispatcher) : ObservableObject
{
    public void PlaceQueens(IEnumerable<Position> positions)
    {
        ClearImages();

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

    public string QueenImagePath { get; } = @"..\..\Images\WhiteQueen.png";

    [ObservableProperty]
    private ObservableCollection<SquareViewModel> squares = [];

    [ObservableProperty]
    private double _windowWidth;

    [ObservableProperty]
    private double _windowHeight;

    public void CreateSquares(int boardSize)
    {
        Squares.Clear();
        var width = WindowWidth / boardSize;
        var height = width;

        for (var i = 0; i < boardSize; i++)
        {
            for (var j = 0; j < boardSize; j++)
            {
                var pos = new Position(i, j);
                var square = new SquareViewModel(pos, FindColor(pos))
                {
                    ImagePath = null!,
                    Height = height,
                    Width = width,
                };

                Squares.Add(square);
            }
        }
    }

    private void ClearImages() =>
        Squares.ToList().ForEach(sq => sq.ImagePath = null!);

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
