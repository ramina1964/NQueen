namespace NQueen.ConsoleApp.Commands;

public record MenuState
{
    public bool ExitRequested { get; set; }

    public int BlankInputCount { get; set; }

    public bool EnableParallelization { get; set; } = true;
}

// Todo: Use input validation here, otherwise move input validation into ValidationHelper.
// Todo: Use constants for menu options, messages, etc from Utils/Constants.cs

public partial class DispatchCommands
{
    public static void RunInteractiveMenu(IServiceProvider services)
    {
        var state = new MenuState();
        Console.WriteLine($"Parallelization is ENABLED by default. Type 'toggle' at any prompt to switch.");
        while (state.ExitRequested == false)
        {
            // Option to toggle parallelization
            Console.WriteLine($"Current parallelization: {(state.EnableParallelization ? "ENABLED" : "DISABLED")}");
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
                    break;

                if (boardSize == -1)
                    break;

                var context = new SimulationContext(boardSize, mode.Value, DisplayMode.Hide, state.EnableParallelization);
                ShowAndHandleResults(services, context, state);

                // Prompt for next action (inline, after summary)
                while (true)
                {
                    Console.Write("Enter a board size, 'back', 'exit', or 'toggle' to switch parallelization: ");
                    var input = Console.ReadLine();
                    if (IsQuitInput(input, state))
                    {
                        state.ExitRequested = true;
                        break;
                    }
                    if (input?.ToLower() == "back")
                        break;
                    if (input?.ToLower() == "toggle")
                    {
                        state.EnableParallelization = !state.EnableParallelization;
                        Console.WriteLine($"Parallelization is now {(state.EnableParallelization ? "ENABLED" : "DISABLED")}");
                        continue;
                    }

                    if (int.TryParse(input, out int nextBoardSize) && nextBoardSize >= 1 && nextBoardSize <= 32)
                    {
                        var nextContext = new SimulationContext(nextBoardSize, mode.Value, DisplayMode.Hide, state.EnableParallelization);
                        ShowAndHandleResults(services, nextContext, state);
                        
                        // After running, return to prompt for the next action
                        continue;
                    }
                }

                break;
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
            return null;

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

    private static void ShowAndHandleResults(
        IServiceProvider services, SimulationContext context, MenuState state)
    {
        if (services.GetService(typeof(ISolutionFormatter)) is not ISolutionFormatter formatter)
        {
            Console.WriteLine("Error: ISolutionFormatter service not found.\n");
            state.ExitRequested = true;
            return;
        }

        Console.WriteLine("Simulation started...");
        Console.WriteLine();

        var solver = new BitmaskSolverExtended(
            context.BoardSize, context.SolutionMode, context.DisplayMode, formatter)
        {
            EnableEvents = false
        };

        var results = solver.Solve();

        // Get the summary string and print it at the top level
        var summary = GetSummaryString(context, results);
        Console.WriteLine(summary);
        // Only two blank lines before the next prompt
        Console.WriteLine();
    }

    // This method now only returns the summary string, does not write to the console
    public static string GetSummaryString(
        SimulationContext context, SimulationResults results)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("Summary:");
        sb.AppendLine($"  Board Size      : {NumericUtils.FormatWithSpaceSeparator(context.BoardSize)}");
        sb.AppendLine($"  Mode            : {context.SolutionMode}");
        sb.AppendLine($"  Total Solutions : {NumericUtils.FormatWithSpaceSeparator(results.SolutionsCount)}{(results.IsTruncated ? $" (showing first {results.Solutions.Count})" : string.Empty)}");
        sb.AppendLine($"  Elapsed (sec)   : {NumericUtils.FormatWithSpaceSeparator(results.ElapsedTimeInSec, 2)}");
        sb.AppendLine($"  Memory (MB)     : {NumericUtils.UpdateMemoryUsage()}");
        sb.AppendLine();

        if (results.SolutionsCount == 0)
        {
            sb.AppendLine("No solutions found.");
        }
        else
        {
            var title = SymmetryHelper.SolutionTitle(context.SolutionMode, results.SolutionsCount);
            sb.AppendLine(title);
            sb.AppendLine();

            foreach (var solution in results.Solutions)
            {
                sb.AppendLine($"Solution {solution.Id}: {solution.Details}");
            }
        }
        sb.AppendLine();

        return sb.ToString();
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

    public static Regex RegexSpaces() => _whiteSpacesRegex;

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex genRegEx();

    private static bool IsExitRequested(string val) =>
        val == "exit" || val == "quit" || val == "e" || val == "q" || val == "0";

    private static readonly Regex _whiteSpacesRegex = genRegEx();
}
