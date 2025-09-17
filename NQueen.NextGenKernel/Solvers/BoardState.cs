namespace NQueen.NextGenKernel.Solvers;

public class BoardState(int boardSize)
{
    public int BoardSize { get; } = boardSize;

    public int[] QueenPositions { get; private set; } =
        [.. Enumerable.Repeat(-1, boardSize)];

    public int HalfBoardSize =>
        BoardSize % 2 == 0
        ? BoardSize / 2
        : BoardSize / 2 + 1;

    public void Reset() =>
        QueenPositions = [.. Enumerable.Repeat(-1, BoardSize)];

    public static ValueTask<int> FindValidQueenPositionAsync(
        int columnIndex, int boardSize, Memory<int> queenPositions, CancellationToken cancellationToken,
        int delayInMilliseconds = 0, DisplayMode displayMode = DisplayMode.Hide)
    {
        // Get the span for efficient access
        var queenSpan = queenPositions.Span;

        // Start from the next row after the current position
        for (var rowIndex = queenSpan[columnIndex] + 1; rowIndex < boardSize; rowIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromResult(-1);

            // Inline the IsValidPosition logic for better performance
            bool isValid = true;
            for (var j = 0; j < columnIndex; j++)
            {
                var diffRow = Math.Abs(rowIndex - queenSpan[j]);
                var diffCol = columnIndex - j;
                if (diffRow == 0 || diffRow == diffCol)
                {
                    isValid = false;
                    break;
                }
            }

            if (isValid)
            {
                if (displayMode == DisplayMode.Visualize && delayInMilliseconds > 0)
                {
                    return new ValueTask<int>(Task.Delay(delayInMilliseconds, cancellationToken)
                        .ContinueWith(_ => rowIndex, cancellationToken));
                }

                return ValueTask.FromResult(rowIndex);
            }
        }

        return ValueTask.FromResult(-1);
    }
}
