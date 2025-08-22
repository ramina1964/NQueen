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

    public static async ValueTask<int> FindValidQueenPositionAsync(
        int colIndex, int boardSize, int[] queenPositions, CancellationToken cancellationToken,
        int delayInMilliseconds = 0, DisplayMode displayMode = DisplayMode.Hide)
    {
        var minColIndex = Math.Min(colIndex, boardSize - 1);
        for (var rowIndex = queenPositions[minColIndex] + 1; rowIndex < boardSize; rowIndex++)
        {
            if (cancellationToken.IsCancellationRequested)
                return -1;

            if (IsValidPosition(colIndex, rowIndex, queenPositions))
            {
                if (displayMode == DisplayMode.Visualize && delayInMilliseconds > 0)
                    await Task.Delay(delayInMilliseconds, cancellationToken);

                return rowIndex;
            }
        }

        return -1;
    }

    private static bool IsValidPosition(int colIndex, int rowIndex, int[] queenPositions)
    {
        for (var j = 0; j < colIndex; j++)
        {
            var lhs = Math.Abs(rowIndex - queenPositions[j]);
            var rhs = Math.Abs(colIndex - j);
            if (lhs == 0 || lhs == rhs)
                return false;
        }

        return true;
    }
}
