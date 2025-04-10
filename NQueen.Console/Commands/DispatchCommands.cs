namespace NQueen.ConsoleApp.Commands;

public partial class DispatchCommands(
    ISolver solver,
    IConsoleUtils consoleUtils,
    ICommandProcessor commandProcessor)
{
    public int BoardSize { get; set; }

    public SolutionMode SolutionMode { get; set; }

    public bool IsSingleSolution => SolutionMode == SolutionMode.Single;

    public bool IsUniqueSolution => SolutionMode == SolutionMode.Unique;

    public bool IsAllSolution => SolutionMode == SolutionMode.All;

    public Dictionary<string, bool> Commands { get; set; }

    public Dictionary<string, string> AvailableCommands { get; set; }

    public bool ProcessCommand(string key, string value) =>
        _commandProcessor.ProcessCommand(key, value, this);

    public void ProcessCommandsFromArgs(string[] args) =>
        _commandProcessor.ProcessCommandsFromArgs(args, this);

    public void ProcessCommandsInteractively() =>
        _commandProcessor.ProcessCommandsInteractively(this);

    public void InitCommands()
    {
        Commands = new Dictionary<string, bool>
        {
            [CommandConst.SolutionMode] = false,
            [CommandConst.BoardSize] = false,
            [CommandConst.Run] = false
        };
        AvailableCommands = new Dictionary<string, string>
        {
            [CommandConst.SolutionMode] = HelpCommands.NQueen_Solution_Mode,
            [CommandConst.BoardSize] = HelpCommands.NQUEEN_BOARDSIZE,
        };
    }

    public static void OutputBanner()
    {
        string[] bannerLines = HelpCommands.Banner.Split("\r\n");
        foreach (string line in bannerLines)
        {
            if (line.StartsWith("| NQueen"))
            {
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.Write("|");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(line[1..^1]);
                Console.ForegroundColor = defaultColor;
                Console.WriteLine("|");
            }
            else
            {
                Console.WriteLine(line);
            }
        }
    }

    public void WriteLineColored(ConsoleColor color, string message) =>
        _consoleUtils.WriteLineColored(color, message);

    public void RunSolver()
    {
        _consoleUtils.WriteLineColored(ConsoleColor.Cyan, CommandConst.SolverRunning);
        ProcessCommand(CommandConst.Run, "ok");
        var runAgain = true;
        while (runAgain)
        {
            Console.WriteLine(CommandConst.RunAgainPrompt);
            Console.WriteLine(CommandConst.YesOrNoPrompt);
            var runAgainAnswer = Console.ReadLine().Trim().ToLower();
            if (runAgainAnswer.Equals("yes") || runAgainAnswer.Equals("y"))
            {
                Console.WriteLine();
                ProcessCommand(CommandConst.Run, "ok");
            }
            else
            {
                runAgain = false;
            }
        }
    }

    #region PrivateMethods
    public async Task<bool> RunApp()
    {
        var simulationResult = await _solver
            .GetResultsAsync(BoardSize, SolutionMode, DisplayMode.Hide);

        var noOfSolutions = simulationResult.NoOfSolutions;
        var elapsedTime = simulationResult.ElapsedTimeInSec;
        if (noOfSolutions == 0)
        {
            _consoleUtils.WriteLineColored(ConsoleColor.Blue, $"\n{Utility.NoSolutionMessage}");
            return true;
        }

        var simTitle = $"Summary of the Results for BoardSize =" +
            $"{BoardSize} and DisplayMode = {SolutionMode}";

        _consoleUtils.WriteLineColored(ConsoleColor.Blue, $"\n{simTitle}:");

        _consoleUtils.WriteLineColored(ConsoleColor.Gray, $"Number of solutions found: {noOfSolutions,10}");
        _consoleUtils.WriteLineColored(ConsoleColor.Gray, $"Elapsed time in seconds: {elapsedTime,14}");

        var example = simulationResult.Solutions.FirstOrDefault();
        var solutionTitle = (example == null)
            ? "\nNo Solution Found!"
            : "\nFirst Solution Found - Numbers in parentheses: Column No. and Row No., Starting from the Lower Left Corner:";

        _consoleUtils.WriteLineColored(ConsoleColor.Blue, solutionTitle);
        _consoleUtils.WriteLineColored(ConsoleColor.Yellow, example.Details);
        var board = DispatchUtils.CreateChessBoard(example.QueenPositions);
        _consoleUtils.WriteLineColored(ConsoleColor.Blue, CommandConst.DrawFirstSolution);

        _consoleUtils.WriteLineColored(ConsoleColor.Gray, CommandConst.SetDefaultFonts);
        Console.WriteLine(board);

        return true;
    }

    public bool CheckSolutionMode(string value)
    {
        Console.WriteLine($"Checking SolutionMode with value: {value}");

        if (int.TryParse(value, out int userChoice) == false)
        {
            HelpCommands.ShowExitError(CommandConst.InvalidBoardSize);
            return false;
        }

        SolutionMode = userChoice switch
        {
            0 => SolutionMode.Single,
            1 => SolutionMode.Unique,
            2 => SolutionMode.All,
            _ => throw new ArgumentOutOfRangeException(
                nameof(value), CommandConst.InvalidSolutionMode)
        };

        // Mark the command as processed
        Commands[CommandConst.SolutionMode] = true;

        return true;
    }

    public bool CheckBoardSize(string value)
    {
        var (isValid, size) = DispatchUtils.CheckBoardSize(value, SolutionMode);
        if (isValid)
            BoardSize = size;
        
        return isValid;
    }

    public string GetRequiredCommand()
    {
        var cmd = Commands.Where(e => !e.Value).Select(e => e.Key).FirstOrDefault();
        
        return cmd ?? "";
    }

    #endregion PrivateMethods

    private readonly ISolver _solver = solver
        ?? throw new ArgumentNullException(nameof(solver));

    private readonly IConsoleUtils _consoleUtils = consoleUtils
        ?? throw new ArgumentNullException(nameof(consoleUtils));

    private readonly ICommandProcessor _commandProcessor = commandProcessor
        ?? throw new ArgumentNullException(nameof(_commandProcessor));
}
