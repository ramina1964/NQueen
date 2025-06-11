namespace NQueen.NextGenKernel.Solvers;

public class NQueenBacktracker(
    BoardState board,
    SolverCancellation cancellation,
    Action<int[]> onSolutionFound)
{
    public async Task FindSolutionsAsync(
        SolutionMode mode, int delayMs, DisplayMode displayMode)
    {
        switch (mode)
        {
            case SolutionMode.Single:
                await FindSingleOrUniqueSolutions(0, SolutionMode.Single, delayMs, displayMode);
                break;
            
            case SolutionMode.Unique:
                await FindSingleOrUniqueSolutions(0, SolutionMode.Unique, delayMs, displayMode);
                break;
            
            case SolutionMode.All:
                await FindAllSolutions(0, delayMs, displayMode);
                break;
        }
    }

    private async Task FindSingleOrUniqueSolutions(
        int colNo, SolutionMode solutionMode, int delayMs, DisplayMode displayMode)
    {
        var boardSize = _board.BoardSize;
        var halfBoardSize = _board.HalfBoardSize;
        var queenPositions = _board.QueenPositions;

        while (colNo != -1)
        {
            if (_cancellation.IsCanceled)
                return;

            if (queenPositions[0] == halfBoardSize)
                return;

            if (colNo == boardSize && solutionMode == SolutionMode.Single)
            {
                _solutions.Add((int[])queenPositions.Clone());
                _onSolutionFound?.Invoke(queenPositions);
                if (delayMs > 0)
                    await Task.Delay(delayMs);
                return;
            }
            else if (colNo == boardSize && solutionMode == SolutionMode.Unique)
            {
                _solutions.Add((int[])queenPositions.Clone());
                _onSolutionFound?.Invoke(queenPositions);
                colNo--;
                continue;
            }

            queenPositions[colNo] = FindQueenPosition(colNo);

            if (queenPositions[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (displayMode == DisplayMode.Visualize)
            {
                _onSolutionFound?.Invoke(queenPositions);
                if (solutionMode != SolutionMode.Single && delayMs > 0)
                    await Task.Delay(delayMs);
            }

            colNo++;
        }
    }

    private async Task FindAllSolutions(int colNo, int delayMs, DisplayMode displayMode)
    {
        await FindSingleOrUniqueSolutions(colNo, SolutionMode.Unique, delayMs, displayMode);
        // Optionally, you can add additional logic for updating or reporting all solutions here.
    }

    private int FindQueenPosition(int colNo)
    {
        var boardSize = _board.BoardSize;
        var queenPositions = _board.QueenPositions;

        for (var pos = queenPositions[colNo] + 1; pos < boardSize; pos++)
        {
            if (_board.IsValidPosition(colNo, pos))
            {
                return pos;
            }
        }

        return -1;
    }

    private readonly BoardState _board = board;
    private readonly SolverCancellation _cancellation = cancellation;
    private readonly Action<int[]> _onSolutionFound = onSolutionFound;
    private readonly HashSet<int[]> _solutions =
        new(new SequenceEquality<int>());
}
