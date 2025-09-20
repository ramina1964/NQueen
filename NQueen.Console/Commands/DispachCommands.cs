namespace NQueen.ConsoleApp.Commands;

public record MenuState
{
    public bool ExitRequested { get; set; }

    public int BlankInputCount { get; set; }
}

// Todo: Use input validation here, otherwise move input validation into ValidationHelper.
// Todo: Use constants for menu options, messages, etc.
// Todo: Remove the choice of Solver type when the only option is "Bitmask N-Queen Solver".

public partial class DispatchCommands
{
    public static void RunInteractiveMenu(IServiceProvider services)
    {
        // Display banner at startup
        Console.WriteLine(HelpCommands.Banner);

        var solvers = new[] { "Bitmask" };
        var state = new MenuState();
        var exitRequested = false;

        while (state.ExitRequested == false)
        {
            var solverIndex = ShowSolverMenu(solvers, state);
            if (exitRequested || solverIndex == -1)
                break;

            while (exitRequested == false)
            {
                // Check SolutionMode
                var mode = ShowSolutionModeMenu(state);
                if (exitRequested || mode == null)
                    break;

                // Check BoardSize
                while (exitRequested == false)
                {
                    var boardSize = ShowBoardSizeMenu(state);

                    if (exitRequested || boardSize == -1)
                        break;

                    // Show Results
                    ShowAndHandleResults(services, boardSize, mode.Value, state);
                }
            }
        }
    }

    private static int ShowSolverMenu(string[] solvers, MenuState state)
    {
        state.BlankInputCount = 0;

        Console.WriteLine();
        Console.WriteLine("Select Solver Type:");
        for (int i = 0; i < solvers.Length; i++)
            Console.WriteLine($"  {i + 1}. {solvers[i]}");

        Console.WriteLine("  0. Exit");
        Console.WriteLine();
        Console.Write("Choice: ");
        var solverInput = Console.ReadLine();
        Console.WriteLine();

        if (IsQuitInput(solverInput, state))
        {
            state.ExitRequested = true;
            return -1;
        }

        if (int.TryParse(solverInput, out int solverIndex) == false || solverIndex < 1 ||
            solverIndex > solvers.Length)
        {
            Console.WriteLine("Invalid choice. Try again.\n");
            return -1;
        }

        return solverIndex;
    }

    private static SolutionMode? ShowSolutionModeMenu(MenuState state)
    {
        state.BlankInputCount = 0;
        Console.WriteLine();
        Console.WriteLine("Select Solution Mode:");
        var modes = Enum.GetValues<SolutionMode>();
        for (int i = 0; i < modes.Length; i++)
            Console.WriteLine($"  {i + 1}. {modes[i]}");

        Console.WriteLine("  0. Back to Solver Selection");
        Console.WriteLine();
        Console.Write("Choice: ");
        var modeInput = Console.ReadLine();
        Console.WriteLine();

        if (IsQuitInput(modeInput, state))
        {
            state.ExitRequested = true;
            return null;
        }

        if (modeInput == "0")
        {
            Console.WriteLine();
            return null;
        }

        if (int.TryParse(modeInput, out int modeIndex) == false || modeIndex < 1 ||
            modeIndex > modes.Length)
        {
            Console.WriteLine("Invalid choice. Try again.\n");
            return null;
        }

        return modes[modeIndex - 1];
    }

    private static int ShowBoardSizeMenu(MenuState state)
    {
        state.BlankInputCount = 0;
        Console.WriteLine();
        Console.Write("Enter board size (1-32, or 0 to go back): ");
        var sizeInput = Console.ReadLine();
        Console.WriteLine();

        if (IsQuitInput(sizeInput, state))
        {
            state.ExitRequested = true;
            return -1;
        }

        if (sizeInput == "0")
        {
            Console.WriteLine();
            return -1;
        }

        if (int.TryParse(sizeInput, out int boardSize) == false || boardSize < 1 ||
            boardSize > 32)
        {
            Console.WriteLine("Invalid board size. Try again.\n");
            return -1;
        }

        return boardSize;
    }

    private static void ShowAndHandleResults(IServiceProvider services, int boardSize, SolutionMode mode, MenuState state)
    {
        if (services.GetService(typeof(ISolutionFormatter))
            is not ISolutionFormatter formatter)
        {
            Console.WriteLine("Error: ISolutionFormatter service not found.\n");
            state.ExitRequested = true;
            return;
        }

        // Inform user that the solving process is starting
        Console.WriteLine("Starting simulation...");
        Console.WriteLine();

        var solver = new BitmaskSolverExtended(boardSize, mode, DisplayMode.Hide, formatter);
        var results = solver.Solve();

        PrintResultsSummary(boardSize, mode, results);

        Console.WriteLine();
        Console.WriteLine("" +
            "Press Enter to run again, or type 'back' to change mode, or 'exit'/'quit'/'e'/'q' to quit.");
        
        Console.WriteLine();
        var again = Console.ReadLine();
        Console.WriteLine();
        if (IsQuitInput(again, state))
        {
            state.ExitRequested = true;
            return;
        }

        if (again?.ToLower() == "back")
        {
            Console.WriteLine();
            return;
        }

        Console.WriteLine();
    }

    private static void PrintResultsSummary(int boardSize, SolutionMode mode,
        SimulationResults  results)
    {
        var formattedTotal = NumericUtil.IncFormattedNumber(results.TotalSolutions.ToString());
        var memoryUsage = NumericUtil.UpdateMemoryUsage();
        var elapsedSec = Math.Round(results.ElapsedTimeInSec, 1);
        var elapsedStr = $"{results.ElapsedTimeInSec:0.0} s (rounded: {elapsedSec})";

        Console.WriteLine();
        Console.WriteLine($"Board Size: {boardSize}");
        Console.WriteLine($"Solution Mode: {mode}");
        Console.WriteLine($"No. of Solutions: {formattedTotal}");
        Console.WriteLine($"Elapsed Time: {elapsedStr}");
        Console.WriteLine($"Memory Consumption: {memoryUsage} MB");
        Console.WriteLine();

        // Display some solutions
        Console.WriteLine("Some of Solutions:");
        foreach (var sol in results.Solutions.Take(3))
            Console.WriteLine(string.Join(", ", sol.QueenPositions));
        
        Console.WriteLine();
    }

    private static bool IsQuitInput(string? input, MenuState state)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            state.BlankInputCount++;
            if (state.BlankInputCount >= 2)
                return true;

            return false;
        }

        state.BlankInputCount = 0;
        var val = input.Trim().ToLower();

        return IsExitRequested(val);
    }

    private static readonly Regex _whiteSpacesRegex = genRegEx();

    public static Regex RegexSpaces() => _whiteSpacesRegex;
    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]

    private static partial Regex genRegEx();

    private static bool IsExitRequested(string val) =>
        val == "exit" || val == "quit" || val == "e" || val == "q" || val == "0";
}
