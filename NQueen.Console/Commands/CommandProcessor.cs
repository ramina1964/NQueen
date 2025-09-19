namespace NQueen.ConsoleApp.Commands;

public class CommandProcessor(IConsoleUtils consoleUtils) : ICommandProcessor
{
    // Map normalized user inputs (no spaces, lowercase) to canonical dictionary keys
    private static readonly Dictionary<string, string> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["solutionmode"] = CommandConst.SolutionMode,
        ["solutionsmode"] = CommandConst.SolutionMode, // forgiving typos (optional)
        ["boardsize"] = CommandConst.BoardSize,
        ["board"] = CommandConst.BoardSize,             // optional shorthand
        ["run"] = CommandConst.Run,
        ["bitmask"] = "bitmask"
    };

    private static string Normalize(string key) =>
        key.Replace(" ", "").Trim().ToLowerInvariant();

    public async Task<bool> ProcessCommand(string key, string value, DispatchCommands dispatch)
    {
        key = key.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            HelpCommands.ShowExitError(CommandConst.CommandEmptyError);
            return false;
        }

        var normalized = Normalize(key);

        if (!KeyMap.TryGetValue(normalized, out var canonical))
        {
            Console.WriteLine($"Unknown command: {key}");
            return false;
        }

        switch (canonical)
        {
            case "bitmask":
                dispatch.RunBitmaskSolver();
                dispatch.Commands["bitmask"] = true;
                return true;

            case var k when k == CommandConst.SolutionMode:
                {
                    var ok = dispatch.CheckSolutionMode(value);
                    if (ok) dispatch.Commands[CommandConst.SolutionMode] = true;
                    return ok;
                }

            case var k when k == CommandConst.BoardSize:
                {
                    var ok = dispatch.CheckBoardSize(value);
                    if (ok) dispatch.Commands[CommandConst.BoardSize] = true;
                    return ok;
                }

            case var k when k == CommandConst.Run:
                await dispatch.RunApp();
                dispatch.Commands[CommandConst.Run] = true;
                return true;

            default:
                Console.WriteLine($"Unknown command: {key}");
                return false;
        }
    }

    public async Task ProcessCommandsFromArgs(string[] args, DispatchCommands dispatch)
    {
        for (var i = 0; i < args.Length; i++)
        {
            (string rawKey, string value) = DispatchUtils.ParseInput(args[i]);
            var ok = await ProcessCommand(rawKey, value, dispatch);
            if (ok)
            {
                var normalized = Normalize(rawKey);
                if (KeyMap.TryGetValue(normalized, out var canonical))
                {
                    if (canonical == CommandConst.BoardSize)
                        dispatch.BoardSize = Convert.ToByte(value);
                }
            }
        }

        if (dispatch.GetRequiredCommand() == CommandConst.Run)
        {
            _consoleUtils.WriteLineColored(ConsoleColor.Cyan, CommandConst.SolverRunning);
            await ProcessCommand(CommandConst.Run, "ok", dispatch);
        }
    }

    public async Task ProcessCommandsInteractively(DispatchCommands dispatch)
    {
        Console.WriteLine("Available commands:");
        foreach (var cmd in dispatch.AvailableCommands)
            Console.WriteLine($"  {cmd.Key} - {cmd.Value}");
        Console.WriteLine("Type a command (or 'exit'):");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null) continue;
            if (line.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            // Allow bare commands without '=' (e.g., run, bitmask)
            string key, value;
            if (line.Contains('='))
            {
                (key, value) = DispatchUtils.ParseInput(line);
            }
            else
            {
                key = line.Trim();
                value = string.Empty;
            }

            await ProcessCommand(key, value, dispatch);
        }
    }

    private readonly IConsoleUtils _consoleUtils = consoleUtils
        ?? throw new ArgumentNullException(nameof(consoleUtils));
}