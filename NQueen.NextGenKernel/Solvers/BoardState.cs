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

    public bool IsValidPosition(int col, int row)
    {
        for (var j = 0; j < col; j++)
        {
            var lhs = Math.Abs(row - QueenPositions[j]);
            var rhs = Math.Abs(col - j);
            if (lhs == 0 || lhs == rhs)
                return false;
        }

        return true;
    }
}
