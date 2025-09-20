namespace NQueen.ConsoleApp.Commands;

public record MenuState
{
    public bool ExitRequested { get; set; }

    public int BlankInputCount { get; set; }
}

// Todo: Use input validation here, otherwise move input validation into ValidationHelper.
// Todo: Use constants for menu options, messages, etc.

public partial class DispatchCommands
{
    public static void RunInteractiveMenu(IServiceProvider services)
    {
        var state = new MenuState();

        while (state.ExitRequested == false)
        {
            // Select Solution Mode (Bitmask solver is implicit)
            var mode = ShowSolutionModeMenu(state);
            if (state.ExitRequested)
                break;

            if (mode == null)
            {
                // User chose 0 (Exit)
                state.ExitRequested = true;
                break;
            }

            // Board size selection loop for chosen mode
            while (state.ExitRequested == false)
            {
                var boardSize = ShowBoardSizeMenu(state);
                if (state.ExitRequested)
                    break; // quit input
                if (boardSize == -1)
                    break; // user entered 0 -> back to mode selection

                ShowAndHandleResults(services, boardSize, mode.Value, state);
            }
        }
    }

    private static SolutionMode? ShowSolutionModeMenu(MenuState state)
    {
        state.BlankInputCount = 0;
        Console.WriteLine();
        Console.WriteLine("Select Solution Mode (Bitmask solver):");
        var modes = Enum.GetValues<SolutionMode>();
        for (int i = 0; i < modes.Length; i++)
            Console.WriteLine($"  {i + 1}. {modes[i]}");

        Console.WriteLine("  0. Exit");
        Console.Write("Choice: ");
        var modeInput = Console.ReadLine();
        Console.WriteLine();

        if (IsQuitInput(modeInput, state))
        {
            state.ExitRequested = true;
            return null;
        }

        if (modeInput == "0")
            return null; // signal exit

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
        Console.Write("Enter board size (1-32, or 0 to go back): ");
        var sizeInput = Console.ReadLine();

        if (IsQuitInput(sizeInput, state))
        {
            state.ExitRequested = true;
            return -1;
        }

        if (sizeInput == "0")
        {
            Console.WriteLine();
            return -1; // back to mode selection
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
        var formatter = services.GetService(typeof(ISolutionFormatter)) as ISolutionFormatter;
        if (formatter == null)
        {
            Console.WriteLine("Error: ISolutionFormatter service not found.\n");
            state.ExitRequested = true;
            return;
        }

        Console.WriteLine("Starting Simulation...");
        Console.WriteLine();

        var solver = new BitmaskSolverExtended(boardSize, mode, DisplayMode.Hide, formatter);
        var results = solver.Solve();

        // (Optional) Could display a summary here if desired in future
        Console.WriteLine();
        Console.WriteLine("Press Enter to run again, or type 'back' to change mode, or 'exit'/'quit'/'e'/'q' to quit.");
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
            return; // back to board size selection loop -> will break and reselect mode
        }

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
    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex genRegEx();

    private static bool IsExitRequested(string val) =>
        val == "exit" || val == "quit" || val == "e" || val == "q" || val == "0";
}
