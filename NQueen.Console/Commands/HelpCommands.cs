namespace NQueen.ConsoleApp.Commands;

public static class HelpCommands
{
    public static void ProcessHelpCommand(string cmd)
    {
        cmd = cmd.Trim().ToUpper();
        string[] parts = cmd.Split(" ");
        if (parts.Length != 2)
        {
            Console.WriteLine();
            ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "AVAILABLE SUBCOMMANDS");
            DumpAllHelp();
        }
        else
        {
            switch (parts[1])
            {
                case CommandConstants.SolutionMode:
                    DumpHelpText(NQUEEN_HELP_SOLUTIONMODE);
                    break;

                case CommandConstants.BoardSize:
                    DumpHelpText(NQUEEN_HELP_BOARDSIZE);
                    break;

                default:
                    DispatchCommands.ShowExitError(
                        $"Unrecognized command {parts[1]}, try {VALID_COMMANDS}");
                    break;
            }
        }
    }

    // Todo: See if you can insert CommandConstants values into the following string.
    public const string VALID_COMMANDS = $"BOARDSIZE, SOLUTIONMODE";
    public const string COMMANDEXAMPLE = "BOARDSIZE = 8 SOLUTIONMODE = 2";

    public const string NQUEEN_HELP_SOLUTIONMODE =
        @"  SOLUTIONMODE - Values one of the following: 0 - 'Single', 1 - 'Unique', 2 - 'All'";

    public static readonly string NQUEEN_HELP_BOARDSIZE =
        @$"  BOARDSIZE   - Whole Numbers in the Range:
                [1, {Utility.MaxBoardSizeForSingleSolution}] for 'Single',
                [1, {Utility.MaxBoardSizeForUniqueSolutions}] for 'Unique',
                [1, {Utility.MaxBoardSizeForAllSolutions}] for 'All' Solutions";

    public static readonly string NQUEEN_SOLUTIONMODE =
        @" Values one of the following: 0 - 'Single', 1 - 'Unique', or 2 - 'All'";

    public static readonly string NQUEEN_BOARDSIZE =
        @$" Whole Numbers in the Range:
                [1, {Utility.MaxBoardSizeForSingleSolution}] for 'Single',
                [1, {Utility.MaxBoardSizeForUniqueSolutions}] for 'Unique',
                [1, {Utility.MaxBoardSizeForAllSolutions}] for 'All' Solutions";

    private static void DumpAllHelp()
    {
        DumpHelpText(NQUEEN_HELP_SOLUTIONMODE);
        DumpHelpText(NQUEEN_HELP_BOARDSIZE);
    }

    private static void DumpHelpText(string text)
    {
        var index = 0;
        foreach (string line in text.Split("\n"))
        {
            if (index++ == 0)
                ConsoleUtils.WriteLineColored(ConsoleColor.Yellow, line);
            else
                Console.WriteLine(line);
        }
    }
}
