namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine(
    BoardState board,
    SolverCancellation cancellation,
    Action<int[]> onSolutionFound)
{
    private async Task FindSingleOrUniqueSolutions(
        int colNo, SM solutionMode, int delayInMs, DP displayMode)
    {
        var boardSize = _board.BoardSize;
        var halfBoardSize = _board.HalfBoardSize;
        var queenPositions = _board.QueenPositions;
        int iteration = 0;

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
                if (delayInMs > 0)
                    await Task.Delay(delayInMs);
                await Task.Yield();

                return;
            }
            else if (colNo == boardSize && solutionMode == SolutionMode.Unique)
            {
                _solutions.Add((int[])queenPositions.Clone());
                _onSolutionFound?.Invoke(queenPositions);
                colNo--;
                continue;
            }

            queenPositions[colNo] = await FindQueenPositionAsync(colNo, delayInMs, displayMode);

            if (queenPositions[colNo] == -1)
            {
                colNo--;
                continue;
            }

            if (displayMode == DisplayMode.Visualize)
            {
                _onSolutionFound?.Invoke(queenPositions);
                if (solutionMode != SolutionMode.Single && delayInMs > 0)
                    await Task.Delay(delayInMs);

                await Task.Yield();
            }

            // Yield every 1000 iterations to keep UI responsive even in Hide mode
            if (++iteration % 1000 == 0)
                await Task.Yield();

            colNo++;
        }
    }

    private async Task FindAllSolutions(int colNo, int delayInMs, DisplayMode displayMode)
    {
        await FindSingleOrUniqueSolutions(colNo, SolutionMode.Unique, delayInMs, displayMode);
        await Task.Yield();
    }

    private async Task<int> FindQueenPositionAsync(int colNo, int delayInMs, DisplayMode displayMode)
    {
        var boardSize = _board.BoardSize;
        var queenPositions = _board.QueenPositions;
        int iteration = 0;
        for (var pos = queenPositions[colNo] + 1; pos < boardSize; pos++)
        {
            if (_board.IsValidPosition(colNo, pos))
            {
                if (displayMode == DisplayMode.Visualize && delayInMs > 0)
                {
                    await Task.Delay(delayInMs);
                    await Task.Yield();
                }
                return pos;
            }

            // Yield every 1000 iterations for very large boards
            if (++iteration % 1000 == 0)
                await Task.Yield();
        }

        return -1;
    }


    private readonly BoardState _board = board;

    private readonly SolverCancellation _cancellation = cancellation;
    
    private readonly Action<int[]> _onSolutionFound = onSolutionFound;
    
    private readonly HashSet<int[]> _solutions =
        new(new SequenceEquality<int>());
}
