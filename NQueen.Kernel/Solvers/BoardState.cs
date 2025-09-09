namespace NQueen.Kernel.Solvers;

public class BoardState(int boardSize)
{
    // Properties
    public int BoardSize { get; } = boardSize;

    public Memory<int> QueenPositions { get; private set; } =
        new Memory<int>(new int[boardSize]);

    public int HalfBoardSize =>
        BoardSize % 2 == 0
        ? BoardSize / 2
        : BoardSize / 2 + 1;

    // Methods
    public void Reset() =>
        QueenPositions.Span.Fill(-1);

    public static async ValueTask<int> TryFindValidPosition(
        int colIndex,
        int boardSize,
        Memory<int> queenPositions,
        CancellationToken cancellationToken,
        int delayInMilliseconds = 0,
        DisplayMode displayMode = DisplayMode.Hide)
    {
        var startRow = 0;

        while (startRow < boardSize)
        {
            if (cancellationToken.IsCancellationRequested)
                return -1;

            if (IsPositionValid(colIndex, startRow, queenPositions))
            {
                if (displayMode == DisplayMode.Visualize && delayInMilliseconds > 0)
                    await Task.Delay(delayInMilliseconds, cancellationToken);
                
                return startRow;
            }

            startRow++;
        }

        return -1;
    }

    private static bool IsPositionValid(int colIndex, int rowIndex, Memory<int> queenPositions)
    {
        var queenSpan = queenPositions.Span;
        for (var j = 0; j < colIndex; j++)
        {
            var placedRow = queenSpan[j];
            if (placedRow == -1)
                continue;

            var rowDifference = Math.Abs(rowIndex - placedRow);
            var colDifference = colIndex - j;

            if (rowDifference == 0 || rowDifference == colDifference)
                return false;
        }

        return true;
    }
}
