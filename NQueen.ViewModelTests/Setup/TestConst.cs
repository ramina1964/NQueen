namespace NQueen.ViewModelTests.Setup;

public static class TestConst
{
    // Progress-related error messages
    public const string ProgressHiddenError =
        "Progress bar should be hidden after simulation";

    public const string ProgressValueUpdateError =
        "Progress value should update during simulation";

    public const string ProgressLabelUpdateError =
        "Progress label should update during simulation";

    // Chessboard-related error messages
    public const string ChessboardNotPopulatedError =
        "Chessboard squares should be populated";

    public const string IncorrectQueenPlacementError =
        "8 queens should be placed on the chessboard for an 8x8 board";

    // Solutions-related error messages
    public const string SolutionNotSelectedError =
        "A solution should be selected after simulation";

    public const string SolutionNumberZeroError =
        "Number of solutions should be updated";

    // Simulation-related error messages
    public const string SimulationNotStoppedError =
        "Simulation should stop when cancel is executed";

    // Save-related error messages
    public const string SaveIdleStateError =
        "Save command should not affect idle state";
}
