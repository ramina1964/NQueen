namespace NQueen.NextGenKernel.Solvers;

// Todo: Find use cases for this record in the production as well as test code.
public record SolverContext(
    int BoardSize,
    int HalfBoardSize,
    int ExpectedCount,
    SolutionMode? SolutionMode = null,
    DisplayMode? DisplayMode = null
);
