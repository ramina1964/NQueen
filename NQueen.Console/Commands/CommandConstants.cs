namespace NQueen.ConsoleApp.Commands;

public static class CommandConst
{
    public const string Run = "Run";
    public const string SolutionMode = "Solution Mode";
    public const string BoardSize = "Board Size";

    public const string Help = "Help";
    public const string Exit = "Exit";
    public const string DisplayMode = "displaymode";
    public const string Delay = "Delay";

    public const string InvalidCommand = "Invalid command. Please try again.";
    public const string CommandEmptyError = "Command key cannot be empty.";
    public const string InvalidBoardSize = "Invalid Board Size. Please Try again.";
    public const string InvalidSolutionMode = "Invalid Solution Mode. Please try 0, 1, or 2.";

    public const string NoSolutionMessage = "No solution found.";
    public const string EnterCommand = "Enter a command: ";
    public const string SolverRunning = "Solver is running...\n";
    public const string RunAgainPrompt = "Run again to debug memory usage? Yes or No";

    // Messages about the board size
    public const string SizeTooLargeForSingleSolution =
        "BoardSize is too large for a single solution.";

    public const string SizeTooLargeForUniqueSolutions =
        "BoardSize is too large for unique solutions.";

    public const string SizeTooLargeForAllSolutions =
        "BoardSize is too large for all solutions.";

    public const string YesOrNoPrompt = "\tYes or No\n";

    public const string DrawFirstSolution = "\nDrawing of first solution:\n";

    public const string SetDefaultFonts =
        "\tIMPORTANT - In this console window, you need to set the default fonts to SimSun-ExtB in order to show unicode characters.\n";
}
