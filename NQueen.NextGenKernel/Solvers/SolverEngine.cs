namespace NQueen.NextGenKernel.Solvers;

public class SolverEngine(
    BoardState board,
    SolverCancellation cancellation,
    Action<int[]> onSolutionFound)
{
    private readonly BoardState _board = board;

    private readonly SolverCancellation _cancellation = cancellation;
}
