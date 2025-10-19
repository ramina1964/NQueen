namespace NQueen.ConsoleApp.Commands;

/// <summary>
/// Interactive menu for NQueen solver.
/// This app always shows both the total count of unique solutions and a few example solutions (up to a cap).
/// This approach is memory-efficient and informative for all board sizes.
/// </summary>
public partial class DispatchCommands
{
    public static void RunInteractiveMenu(IServiceProvider services)
    {
        var state = new MenuState();
        bool infoShown = false;
        while (state.ExitRequested == false)
        {
            if (!infoShown)
            {
                Console.WriteLine("Output mode: This app will show the total count of unique solutions and a few example solutions (up to 5).\nThe total count is computed using a fast, memory-efficient algorithm.\nThis is the recommended mode for large boards, as it is memory-efficient and informative.");
                infoShown = true;
            }
            var mode = ShowSolutionModeMenu(state);
            if (state.ExitRequested)
                break;
            if (mode == null)
            {
                state.ExitRequested = true;
                break;
            }
            while (state.ExitRequested == false)
            {
                var boardSize = ShowBoardSizeMenu(state);
                if (state.ExitRequested) break;
                if (boardSize == -1) break;
                var context = new SimulationContext(boardSize, mode.Value, DisplayMode.Hide);
                bool useCountOnly = false;
                if (mode == SolutionMode.Unique || mode == SolutionMode.All)
                {
                    Console.Write($"Use CountOnly mode for {mode} solutions? (y/n): ");
                    var countOnlyInput = Console.ReadLine()?.Trim().ToLower();
                    useCountOnly = countOnlyInput == "y" || countOnlyInput == "yes";
                }
                ShowCombinedResults(services, context, state, useCountOnly, mode.Value);
                while (true)
                {
                    Console.Write("Enter a board size, 'back' ('b') or 'exit' ('e'): ");
                    var input = Console.ReadLine()?.Trim().ToLower();
                    if (IsQuitInput(input, state))
                    {
                        state.ExitRequested = true;
                        break;
                    }
                    if (input == "back" || input == "b")
                        break;
                    if (int.TryParse(input, out int nextBoardSize) &&
                        nextBoardSize >= 1 && nextBoardSize <= BoardSettings.MaxBitmaskBoardSize)
                    {
                        var nextContext = new SimulationContext(nextBoardSize, mode.Value, DisplayMode.Hide);
                        ShowCombinedResults(services, nextContext, state, useCountOnly, mode.Value);
                        continue;
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Shows both the total count and example solutions using UniqueSolutionExamplesAndCountSolver.
    /// </summary>
    private static void ShowCombinedResults(
        IServiceProvider services, SimulationContext context, MenuState state, bool useCountOnly = false, SolutionMode mode = SolutionMode.Single)
    {
        if (services.GetService(typeof(ISolutionFormatter)) is not ISolutionFormatter formatter)
        {
            Console.WriteLine("Error: ISolutionFormatter service not found.\n");
            state.ExitRequested = true;
            return;
        }
        SimulationResults results;
        if (mode == SolutionMode.Unique)
        {
            var solver = new BitmaskSolver(context.BoardSize, SolutionMode.Unique, context.DisplayMode, formatter, SimulationSettings.MaxDisplayedCount);
            solver.UseCountOnlyUniqueMode = useCountOnly;
            results = solver.Solve();
        }
        else if (mode == SolutionMode.All)
        {
            var solver = new BitmaskSolver(context.BoardSize, SolutionMode.All, context.DisplayMode, formatter, SimulationSettings.MaxDisplayedCount);
            solver.UseCountOnlyAllMode = useCountOnly;
            results = solver.Solve();
        }
        else
        {
            var solver = new BitmaskSolver(context.BoardSize, mode, context.DisplayMode, formatter, SimulationSettings.MaxDisplayedCount);
            results = solver.Solve();
        }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Summary:");
        sb.AppendLine($"  Board Size      : {NumericUtils.FormatWithSpaceSeparator(context.BoardSize)}");
        sb.AppendLine($"  Mode            : {context.SolutionMode}");
        sb.AppendLine($"  Total Solutions : {results.SolutionsCount:N0}");
        sb.AppendLine($"  Elapsed (sec)   : {results.ElapsedTimeInSec:N1}");
        sb.AppendLine($"  Memory (MB)     : {NumericUtils.UpdateMemoryUsage()}");
        sb.AppendLine();
        if (results.Solutions.Count == 0)
        {
            // Only show 'No example solutions found.' if NOT CountOnly mode
            if (mode == SolutionMode.All && !useCountOnly)
                sb.AppendLine("No example solutions found.");
        }
        else
        {
            sb.AppendLine($"Showing up to {SimulationSettings.MaxDisplayedCount} example solutions:");
            foreach (var solution in results.Solutions)
            {
                sb.AppendLine($"Solution {solution.Id}: {solution.Details}");
            }
        }
        sb.AppendLine();
        Console.WriteLine(sb.ToString());
        // Ensure exactly 2 blank lines after summary
        Console.WriteLine();
        Console.WriteLine();
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

        if (int.TryParse(modeInput, out int modeIndex) == false ||
            modeIndex < 1 || modeIndex > modes.Length)
        {
            Console.WriteLine("Invalid choice. Try again.\n");
            return null;
        }
        return modes[modeIndex - 1];
    }

    private static int ShowBoardSizeMenu(MenuState state)
    {
        state.BlankInputCount = 0;
        Console.Write($"Enter board size (1-{BoardSettings.MaxBitmaskBoardSize}, or 0 to go back): ");
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
        if (int.TryParse(sizeInput, out int boardSize) == false ||
            boardSize < 1 || boardSize > BoardSettings.MaxBitmaskBoardSize)
        {
            Console.WriteLine("Invalid board size. Try again.\n");
            return -1;
        }
        return boardSize;
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
