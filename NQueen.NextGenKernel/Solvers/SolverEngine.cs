namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine(
    BoardState board,
    SolverCancellation cancellation,
    Action<int[]> onSolutionFound)
{
    private async Task FindSingleOrUniqueSolutions(
        int colIndex, SolutionMode solutionMode, int delayInMs, DisplayMode displayMode)
    {
        var boardSize = _board.BoardSize;
        var halfBoardSize = _board.HalfBoardSize;
        var queenPositions = _board.QueenPositions;

        while (colIndex != -1)
        {
            if (_cancellation.IsCanceled)
                return;

            if (queenPositions[0] == halfBoardSize)
                return;

            if (colIndex == boardSize && solutionMode == SolutionMode.Single)
            {
                _solutions.Add((int[])queenPositions.Clone());
                _onSolutionFound?.Invoke(queenPositions);
                if (delayInMs > 0)
                    await Task.Delay(delayInMs);

                return;
            }
            else if (colIndex == boardSize && solutionMode == SolutionMode.Unique)
            {
                _solutions.Add((int[])queenPositions.Clone());
                _onSolutionFound?.Invoke(queenPositions);
                colIndex--;
                continue;
            }

            queenPositions[colIndex] = await FindQueenPositionAsync(colIndex, delayInMs, displayMode);

            if (queenPositions[colIndex] == -1)
            {
                colIndex--;
                continue;
            }

            if (displayMode == DisplayMode.Visualize)
            {
                _onSolutionFound?.Invoke(queenPositions);
                if (solutionMode != SolutionMode.Single && delayInMs > 0)
                    await Task.Delay(delayInMs);
            }

            colIndex++;
        }
    }

    private async Task<int> FindQueenPositionAsync(int colIndex, int delayInMs, DisplayMode displayMode)
    {
        var boardSize = _board.BoardSize;
        var queenPositions = _board.QueenPositions;
        for (var rowIndex = queenPositions[colIndex] + 1; rowIndex < boardSize; rowIndex++)
        {
            if (_board.IsValidPosition(colIndex, rowIndex))
            {
                if (displayMode == DisplayMode.Visualize && delayInMs > 0)
                    await Task.Delay(delayInMs);

                return rowIndex;
            }
        }

        return -1;
    }


    private readonly BoardState _board = board;

    private readonly SolverCancellation _cancellation = cancellation;
    
    private readonly Action<int[]> _onSolutionFound = onSolutionFound;
    
    private readonly HashSet<int[]> _solutions = new(new IntArrayComparer());
}
