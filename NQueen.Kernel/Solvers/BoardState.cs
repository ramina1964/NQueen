namespace NQueen.Kernel.Solvers;

public class BoardState(int boardSize)
{
    // Properties
    public int BoardSize { get; } = boardSize;

    public Memory<int> QueenPositions { get; private set; } =
        new Memory<int>([.. Enumerable.Repeat(-1, boardSize)]);

    public int HalfBoardSize =>
        BoardSize % 2 == 0
        ? BoardSize / 2
        : BoardSize / 2 + 1;

    // Methods
    public void Reset() =>
        QueenPositions = new Memory<int>([.. Enumerable.Repeat(-1, BoardSize)]);

    public static async ValueTask<int> TryFindValidPosition(
        int colIndex,
        int boardSize,
        Memory<int> queenPositions,
        CancellationToken cancellationToken,
        int delayInMilliseconds = 0,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        var minColIndex = Math.Min(colIndex, boardSize - 1);

        return await FindRowAsync(
            minColIndex,
            boardSize,
            rowIndex => IsPositionValid(colIndex, rowIndex, queenPositions),
            delayInMilliseconds,
            displayMode,
            cancellationToken);
    }

    private static async ValueTask<int> FindRowAsync(
        int startRow,
        int maxRow,
        Func<int, bool> isValid,
        int delayInMilliseconds,
        DisplayMode displayMode,
        CancellationToken cancellationToken)
    {
        for (var rowIndex = startRow + 1; rowIndex < maxRow; rowIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
                return -1;

            if (isValid(rowIndex))
            {
                await DelayIfVisualizing(displayMode, delayInMilliseconds, cancellationToken);
                return rowIndex;
            }
        }

        return -1;
    }

    private static bool IsPositionValid(int colIndex, int rowIndex, Memory<int> queenPositions)
    {
        var queenSpan = queenPositions.Span; // Access the Span here
        for (var j = 0; j < colIndex; j++)
        {
            var rowDifference = Math.Abs(rowIndex - queenSpan[j]);
            var colDifference = Math.Abs(colIndex - j);
            if (rowDifference == 0 || rowDifference == colDifference)
                return false;
        }

        return true;
    }

    private static async Task DelayIfVisualizing(DisplayMode displayMode, int delayInMilliseconds, CancellationToken cancellationToken)
    {
        if (displayMode == DisplayMode.Visualize && delayInMilliseconds > 0)
        {
            await Task.Delay(delayInMilliseconds, cancellationToken);
        }
    }
}
