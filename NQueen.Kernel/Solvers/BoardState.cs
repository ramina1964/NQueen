namespace NQueen.Kernel.Solvers;

public class BoardState(int boardSize)
{
    // Properties
    public int BoardSize { get; } = boardSize;

    public Memory<int> QueenPositions { get; private set; } =
        new Memory<int>(new int[boardSize]);

    public int HalfBoardSize => (BoardSize + 1) / 2;

    // Methods
    public void Reset() =>
        QueenPositions.Span.Fill(-1);

    public static bool IsPositionValid(int columnIndex, int rowIndex, Memory<int> queenPositions)
    {
        var queenSpan = queenPositions.Span;
        for (var j = 0; j < columnIndex; j++)
        {
            var placedRow = queenSpan[j];
            if (placedRow == -1)
                continue;

            var rowDifference = Math.Abs(rowIndex - placedRow);
            var colDifference = columnIndex - j;

            if (rowDifference == 0 || rowDifference == colDifference)
                return false;
        }

        return true;
    }
}
