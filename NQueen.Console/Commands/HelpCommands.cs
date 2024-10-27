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
            ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "Available Subcommands");
            DumpAllHelp();
        }
        else
        {
            switch (parts[1])
            {
                case CommandConstants.SolutionMode:
                    DumpHelpText(NQueen_Help_Solution_Mode);
                    break;

                case CommandConstants.BoardSize:
                    DumpHelpText(NQueen_Help_Board_Size);
                    break;

                default:
                    DispatchCommands.ShowExitError(
                        $"Unrecognized command {parts[1]}, try {Valid_Commands}");
                    break;
            }
        }
    }

    public const string Valid_Commands = 
        $"{CommandConstants.BoardSize}, {CommandConstants.SolutionMode}";

    public const string Command_Example = 
        $"{CommandConstants.BoardSize} = 8 {CommandConstants.SolutionMode} = 2";

    public const string NQueen_Help_Solution_Mode =
        @"  SOLUTIONMODE - Values one of the following: 0 - 'Single', 1 - 'Unique', 2 - 'All'";

    public static readonly string NQueen_Help_Board_Size =
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
        DumpHelpText(NQueen_Help_Solution_Mode);
        DumpHelpText(NQueen_Help_Board_Size);
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
