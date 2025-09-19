namespace NQueen.ConsoleApp.Commands;

public partial class DispatchCommands(
    ISolverPruning solver,
    IConsoleUtils consoleUtils,
    ICommandProcessor commandProcessor,
    ISolutionFormatter formatter)
{
    public int BoardSize { get; set; }
    public SolutionMode SolutionMode { get; set; }

    public Dictionary<string, bool> Commands { get; set; } = [];

    public Dictionary<string, string> AvailableCommands { get; set; } = [];

    public bool IsConfigured =>
        Commands.TryGetValue(CommandConst.SolutionMode, out var sm) && sm &&
        Commands.TryGetValue(CommandConst.BoardSize, out var bs) && bs;

    public async Task<bool> ProcessCommand(string key, string value) =>
        await _commandProcessor.ProcessCommand(key, value, this);

    public async Task ProcessCommandsFromArgs(string[] args) =>
        await _commandProcessor.ProcessCommandsFromArgs(args, this);

    public async Task ProcessCommandsInteractively() =>
        await _commandProcessor.ProcessCommandsInteractively(this);

    public void InitCommands()
    {
        Commands = new Dictionary<string, bool>
        {
            [CommandConst.SolutionMode] = false,
            [CommandConst.BoardSize] = false,
            [CommandConst.Run] = false,
            ["bitmask"] = false
        };
        AvailableCommands = new Dictionary<string, string>
        {
            [CommandConst.SolutionMode] = HelpCommands.NQueen_Solution_Mode,
            [CommandConst.BoardSize] = HelpCommands.NQUEEN_BOARDSIZE,
            ["bitmask"] = "Run the high-performance bitmask N-Queens solver"
        };
    }

    public void WriteLineColored(ConsoleColor color, string message) =>
        _consoleUtils.WriteLineColored(color, message);

    public async Task RunSolver()
    {
        if (!EnsureConfigurationInteractive()) return;
        _consoleUtils.WriteLineColored(ConsoleColor.Cyan, CommandConst.SolverRunning);
        await ProcessCommand(CommandConst.Run, "ok");
    }

    private bool EnsureConfigurationInteractive()
    {
        if (!Commands[CommandConst.SolutionMode])
            PromptSolutionModeInteractive();

        if (!Commands[CommandConst.BoardSize])
            PromptBoardSizeInteractive();

        return Commands[CommandConst.SolutionMode] && Commands[CommandConst.BoardSize];
    }

    public void PromptSolutionModeInteractive()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Select Solution Mode:");
            Console.WriteLine("  0 - Single");
            Console.WriteLine("  1 - Unique");
            Console.WriteLine("  2 - All");
            Console.Write("Enter choice (0/1/2): ");
            var input = Console.ReadLine();
            if (ParsingUtils.TryParseInt(input ?? "", out var value) && value is >= 0 and <= 2)
            {
                SolutionMode = value switch
                {
                    0 => SolutionMode.Single,
                    1 => SolutionMode.Unique,
                    _ => SolutionMode.All
                };
                Commands[CommandConst.SolutionMode] = true;
                Console.WriteLine($"Solution Mode set to: {SolutionMode}");
                break;
            }
            Console.WriteLine("Invalid selection. Try again.");
        }
    }

    public void PromptBoardSizeInteractive()
    {
        while (true)
        {
            Console.WriteLine();
            Console.Write($"Enter Board Size for mode {SolutionMode} (or 'q' to cancel): ");
            var input = Console.ReadLine();
            if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                return;

            if (!string.IsNullOrWhiteSpace(input))
            {
                var validator = new BoardSizeValidator(SolutionMode);
                var result = validator.Validate(input);
                if (result.IsValid)
                {
                    BoardSize = ParsingUtils.ParseIntOrThrow(input);
                    Commands[CommandConst.BoardSize] = true;
                    Console.WriteLine($"Board Size set to: {BoardSize}");
                    break;
                }
            }
            Console.WriteLine("Invalid board size. Try again.");
        }
    }

    public async Task<bool> RunApp()
    {
        var simContext = new SimulationContext(
            BoardSize, SolutionMode, DisplayMode.Hide);

        var simulationResult = await _solver.GetSimResultsAsync(simContext);

        var noOfSolutions = simulationResult.TotalSolutions; // use full total

        var elapsedTime = simulationResult.ElapsedTimeInSec;
        if (noOfSolutions == 0)
        {
            _consoleUtils.WriteLineColored(ConsoleColor.Blue,
                $"\n{ErrorMessages.NoSolutionMessage}");
            return true;
        }

        var simTitle = $"Summary of the Results for BoardSize ={BoardSize} and DisplayMode = {SolutionMode}";
        _consoleUtils.WriteLineColored(ConsoleColor.Blue, $"\n{simTitle}:");

        // Replace old count output with truncated awareness
        var truncatedLine = simulationResult.IsTruncated
            ? $"Number of solutions found: {simulationResult.TotalSolutions} (showing first {simulationResult.Solutions.Count})"
            : $"Number of solutions found: {simulationResult.TotalSolutions}";
        _consoleUtils.WriteLineColored(ConsoleColor.Gray, truncatedLine);

        _consoleUtils.WriteLineColored(ConsoleColor.Gray, $"Elapsed time in seconds: {elapsedTime,14}");

        var example = simulationResult.Solutions.FirstOrDefault();
        var solutionTitle = example == null
            ? "\nNo Solution Found!"
            : "\nFirst Solution Found - Numbers in parentheses: Column No. and Row No., Starting from the Lower Left Corner:";

        _consoleUtils.WriteLineColored(ConsoleColor.Blue, solutionTitle);

        if (example != null)
            _consoleUtils.WriteLineColored(ConsoleColor.Yellow, example.Details);
        else
            _consoleUtils.WriteLineColored(ConsoleColor.Red, "No example solution available.");

        var sample = simulationResult.Solutions.FirstOrDefault();
        if (sample == null) return true;

        var board = DispatchUtils.CreateChessBoard(sample.QueenPositions ?? []);
        _consoleUtils.WriteLineColored(ConsoleColor.Blue, CommandConst.DrawFirstSolution);
        _consoleUtils.WriteLineColored(ConsoleColor.Gray, CommandConst.SetDefaultFonts);
        Console.WriteLine(board);

        return true;
    }

    public bool CheckSolutionMode(string value)
    {
        var ok = ParsingUtils.TryParseInt(value, out int solutionMode);
        if (!ok)
        {
            HelpCommands.ShowExitError(CommandConst.InvalidSolutionMode);
            return false;
        }

        SolutionMode = solutionMode switch
        {
            0 => SolutionMode.Single,
            1 => SolutionMode.Unique,
            2 => SolutionMode.All,
            _ => throw new ArgumentOutOfRangeException(nameof(value), CommandConst.InvalidSolutionMode)
        };
        Commands[CommandConst.SolutionMode] = true;
        return true;
    }

    public bool CheckBoardSize(string boardSizeText)
    {
        var inputViewModel = new BoardSizeValidator(SolutionMode);
        var isValid = inputViewModel.Validate(boardSizeText).IsValid;
        if (!isValid)
        {
            HelpCommands.ShowExitError(CommandConst.InvalidBoardSize);
            return false;
        }

        BoardSize = ParsingUtils.ParseIntOrThrow(boardSizeText);
        Commands[CommandConst.BoardSize] = true;
        return true;
    }

    public string GetRequiredCommand() =>
        Commands.Where(e => !e.Value).Select(e => e.Key).FirstOrDefault() ?? "";

    public void RunBitmaskSolver()
    {
        if (!EnsureConfigurationInteractive()) return;

        var engine = new BitmaskSolverEngineFull(BoardSize, SolutionMode, DisplayMode.Hide, _formatter);
        var results = engine.Solve();

        Console.WriteLine();
        Console.WriteLine($"BitmaskSolver: N={BoardSize}, Mode={SolutionMode}");

        Console.WriteLine(
            results.IsTruncated
                ? $"Solutions found: {results.TotalSolutions} (showing first {results.Solutions.Count})"
                : $"Solutions found: {results.TotalSolutions}");

        Console.WriteLine($"Elapsed time: {results.ElapsedTimeInSec} sec");

        foreach (var sol in results.Solutions.Take(3))
            Console.WriteLine(string.Join(", ", sol.QueenPositions));
    }

    private readonly ISolverPruning _solver = solver
        ?? throw new ArgumentNullException(nameof(solver));

    private readonly IConsoleUtils _consoleUtils = consoleUtils
        ?? throw new ArgumentNullException(nameof(consoleUtils));

    private readonly ICommandProcessor _commandProcessor = commandProcessor
        ?? throw new ArgumentNullException(nameof(commandProcessor));

    private readonly ISolutionFormatter _formatter = formatter
        ?? throw new ArgumentNullException(nameof(formatter));
}
