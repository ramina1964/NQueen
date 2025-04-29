namespace NQueen.ViewModelTests.Setup;

public static class TestConst
{
    // Group: Chessboard Errors
    public const string ChessboardNotPopulatedError =
        "The chessboard was not populated during the simulation.";

    public const string IncorrectQueenPlacementError =
        "The queen placements on the chessboard are incorrect.";

    // Group: Simulation Errors
    public const string SolutionNotSelectedError =
        "No solution was selected after the simulation.";

    public const string SolutionNumberZeroError =
        "The number of solutions should not be zero after the simulation.";

    public const string SimulationNotStoppedError =
        "The simulation was not stopped when the cancel command was executed.";

    // Group: Save Command Errors
    public const string BoardSizeLabel = "Board Size: ";
    public const string NoOfSolutionsLabel = "Number of Solutions: ";
    public const string ElapsedTimeLabel = "Elapsed Time: ";

    public const string SaveDialogNotShownError =
        "The save file dialog was not shown when the save command was executed.";

    public const string ContentNotSavedError =
        "The content was not saved after the save command was executed.";

    public const string BoardSizeLabelError =
        "The 'Board Size' label is missing in the saved content.";

    public const string NoOfSolsLabelError =
        "The 'Number of Solutions' label is missing in the saved content.";

    public const string ElapsedTimeLabelError =
        "The 'Elapsed Time' label is missing in the saved content.";

    public const string BoardSizeValueError =
        "The board size value in the saved content is incorrect.";

    public const string NoOfSolsValueError =
        "The number of solutions value in the saved content is incorrect.";

    // Group: Visualization Errors
    public const string ChessboardNotPopulatedDuringVisualizationError =
        "The chessboard was not populated during visualization.";

    public const string IncorrectQueenPlacementDuringVisualizationError =
        "The queen placements on the chessboard during visualization are incorrect.";
}
