namespace NQueen.ConsoleApp.Commands;

public class CommandProcessor(IConsoleUtils consoleUtils) : ICommandProcessor
{
    public bool ProcessCommand(string key, string value, DispatchCommands dispatchCommands)
    {
        var returnValue = false;
        key = DispatchCommands.RegexSpaces().Replace(key, " ").Trim();

        if (string.IsNullOrEmpty(key))
        {
            HelpCommands.ShowExitError(CommandConst.CommandEmptyError);
            return false;
        }

        return key switch
        {
            CommandConst.Run => dispatchCommands.RunApp().Result,
            CommandConst.SolutionMode => dispatchCommands.CheckSolutionMode(value),
            CommandConst.BoardSize => dispatchCommands.CheckBoardSize(value),
            _ => returnValue,
        };
    }

    public void ProcessCommandsFromArgs(string[] args, DispatchCommands dispatchCommands)
    {
        for (var i = 0; i < args.Length; i++)
        {
            (string key, string value) = DispatchUtils.ParseInput(args[i]);
            var ok = ProcessCommand(key, value, dispatchCommands);
            if (ok)
            {
                dispatchCommands.Commands[key.ToUpper()] = true;
                if (key.Equals(CommandConst.BoardSize))
                {
                    dispatchCommands.BoardSize = Convert.ToSByte(value);
                }
            }
        }

        if (dispatchCommands.GetRequiredCommand() == CommandConst.Run)
        {
            _consoleUtils.WriteLineColored(ConsoleColor.Cyan, CommandConst.SolverRunning);
            ProcessCommand(CommandConst.Run, "ok", dispatchCommands);
        }
    }

    public void ProcessCommandsInteractively(DispatchCommands dispatchCommands)
    {
        while (dispatchCommands.Commands.Any(e => !e.Value))
        {
            var required = dispatchCommands.GetRequiredCommand();
            if (required == CommandConst.Run)
            {
                dispatchCommands.RunSolver();
                break;
            }

            dispatchCommands.WriteLineColored(ConsoleColor.Cyan, $"Enter a {required} ");
            Console.WriteLine($"\t{dispatchCommands.AvailableCommands[required]}");
            var userInput = Console.ReadLine().Trim().ToLower();

            if (userInput.Equals("help") || userInput.Equals("-h"))
                HelpCommands.ProcessHelpCommand(userInput);
            else
            {
                var ok = ProcessCommand(required, userInput, dispatchCommands);
                if (ok)
                {
                    dispatchCommands.Commands[required] = true;
                    if (required.Trim().Equals(
                        CommandConst.BoardSize,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        dispatchCommands.BoardSize = Convert.ToSByte(userInput);
                    }
                }
            }
        }
    }

    private readonly IConsoleUtils _consoleUtils = consoleUtils
            ?? throw new ArgumentNullException(nameof(consoleUtils));
}