namespace NQueen.ConsoleApp.Commands;

public class DispatchCommands
{
    public static void RunInteractiveMenu(IServiceProvider services)
    {
        // 1. Solver type selection (extendable for more solvers)
        var solvers = new[] { "Bitmask" };
        int solverIndex = 0;
        bool exitRequested = false;
        while (!exitRequested)
        {
            // Solver type menu
            Console.WriteLine("Select Solver Type:");
            for (int i = 0; i < solvers.Length; i++)
                Console.WriteLine($"  {i + 1}. {solvers[i]}");
            Console.WriteLine("  0. Exit");
            Console.Write("Choice: ");
            var solverInput = Console.ReadLine();
            if (solverInput == "0" || solverInput?.ToLower() == "exit" || solverInput?.ToLower() == "quit")
                break;
            if (!int.TryParse(solverInput, out solverIndex) || solverIndex < 1 || solverIndex > solvers.Length)
            {
                Console.WriteLine("Invalid choice. Try again.");
                continue;
            }
            // Only Bitmask for now
            while (!exitRequested)
            {
                // 2. SolutionMode selection
                Console.WriteLine("Select Solution Mode:");
                var modes = Enum.GetValues<SolutionMode>();
                for (int i = 0; i < modes.Length; i++)
                    Console.WriteLine($"  {i + 1}. {modes[i]}");
                Console.WriteLine("  0. Back to Solver Selection");
                Console.Write("Choice: ");
                var modeInput = Console.ReadLine();
                if (modeInput == "0") break;
                if (!int.TryParse(modeInput, out int modeIndex) || modeIndex < 1 || modeIndex > modes.Length)
                {
                    Console.WriteLine("Invalid choice. Try again.");
                    continue;
                }
                var mode = modes[modeIndex - 1];
                // 3. Board size selection
                int boardSize = 0;
                while (!exitRequested)
                {
                    Console.Write("Enter board size (1-32, or 0 to go back): ");
                    var sizeInput = Console.ReadLine();
                    if (sizeInput == "0") break;
                    if (sizeInput?.ToLower() == "exit" || sizeInput?.ToLower() == "quit") { exitRequested = true; break; }
                    if (!int.TryParse(sizeInput, out boardSize) || boardSize < 1 || boardSize > 32)
                    {
                        Console.WriteLine("Invalid board size. Try again.");
                        continue;
                    }
                    // 4. Run solver
                    var formatter = services.GetService(typeof(ISolutionFormatter)) as ISolutionFormatter;
                    if (formatter == null)
                    {
                        Console.WriteLine("Error: ISolutionFormatter service not found.");
                        return;
                    }
                    var solver = new BitmaskSolverEngineFull(
                        boardSize,
                        mode,
                        DisplayMode.Hide,
                        formatter);
                    var results = solver.Solve();
                    Console.WriteLine($"BitmaskSolver: N={boardSize}, Mode={mode}");
                    Console.WriteLine($"Solutions found: {results.Solutions.Count()}");
                    Console.WriteLine($"Elapsed time: {results.ElapsedTimeInSec} sec");
                    foreach (var sol in results.Solutions.Take(3))
                        Console.WriteLine(string.Join(", ", sol.QueenPositions));
                    Console.WriteLine();
                    // 5. Ask for another run or go back
                    Console.WriteLine("Press Enter to run again, or type 'back' to change mode, or 'exit' to quit.");
                    var again = Console.ReadLine();
                    if (again?.ToLower() == "back") break;
                    if (again?.ToLower() == "exit" || again?.ToLower() == "quit") { exitRequested = true; break; }
                }
            }
        }
    }

    // Standard static method for whitespace regex (no partial, no attribute)
    private static readonly Regex _whiteSpacesRegex =
        new(@"\s+", RegexOptions.Compiled);

    public static Regex RegexSpaces() => _whiteSpacesRegex;
}
