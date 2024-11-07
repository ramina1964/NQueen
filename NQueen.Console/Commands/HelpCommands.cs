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
                case CommandConst.SolutionMode:
                    DumpHelpText(NQueen_Solution_Mode);
                    break;

                case CommandConst.BoardSize:
                    DumpHelpText(NQueen_Help_Board_Size);
                    break;

                default:
                    ShowExitError(
                        $"Unrecognized command {parts[1]}, try {Valid_Commands}");
                    break;
            }
        }
    }

    public const string Valid_Commands =
        $"{CommandConst.BoardSize}, {CommandConst.SolutionMode}";

    public const string Command_Example =
        $"{CommandConst.BoardSize} = 8 {CommandConst.SolutionMode} = 2";

    public static readonly string NQueen_Help_Board_Size =
        @$"  Board Size   - Whole Numbers in the following Ranges:
                [1, {Utility.MaxBoardSizeForSingleSolution}] for 'Single',
                [1, {Utility.MaxBoardSizeForUniqueSolutions}] for 'Unique',
                [1, {Utility.MaxBoardSizeForAllSolutions}] for 'All' Solutions";

    public static readonly string NQueen_Solution_Mode =
        @" Values one of the following: 0 - 'Single', 1 - 'Unique', or 2 - 'All'";

    public static readonly string NQUEEN_BOARDSIZE =
        @$" Whole Numbers in the Range:
                [1, {Utility.MaxBoardSizeForSingleSolution}] for 'Single',
                [1, {Utility.MaxBoardSizeForUniqueSolutions}] for 'Unique',
                [1, {Utility.MaxBoardSizeForAllSolutions}] for 'All' Solutions";

    private static void DumpAllHelp()
    {
        DumpHelpText(NQueen_Solution_Mode);
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

    public static void ShowExitError(string errorString)
    {
        ConsoleColor priorColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("ERROR: ");
        Console.ForegroundColor = priorColor;
        Console.WriteLine(errorString);
        Console.WriteLine();
        Environment.Exit(-1);
    }

    public static void ShowHelp()
    {
        Console.WriteLine();
        ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "Available Commands");
        Console.WriteLine($"\t{CommandConst.BoardSize}");
        Console.WriteLine($"\t{CommandConst.SolutionMode}");
        Console.WriteLine();
        ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "Example Command");
        Console.WriteLine($"\t{Command_Example}");
    }

    public const string Banner =
        @"
                |====================================================|
                | NQueen.ConsoleApp - A .NET 8.0 Console Application |
                |                                                    |
                | (c) 2022 - Ramin Anvar and Lars Erik Pedersen      |
                |                                                    |
                | App Developed for Solving N-Queen Problem          |
                | Using the Iterative Backtracking Algorithm         |
                |                                                    |
                | Version 0.90. Use help to list available commands. |
                |                                                    |
                |====================================================|
            ";
}
