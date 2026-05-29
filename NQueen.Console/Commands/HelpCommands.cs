namespace NQueen.ConsoleApp.Commands;

public static class HelpCommands
{
    public const string Valid_Commands =
        $"{CommandConst.BoardSize}, {CommandConst.SolutionMode}, bitmask";

    public const string Command_Example =
        $"{CommandConst.BoardSize} = 8 {CommandConst.SolutionMode} = 2";

    public static readonly string NQueen_Help_Board_Size =
        @$"  Board Size   - Whole Numbers in the following Ranges:
                [1, {BoardSettings.MaxSizeForSingle}] for 'Single',
                [1, {BoardSettings.MaxSizeForUnique}] for 'Unique',
                [1, {BoardSettings.MaxSizeForAll}] for 'All' Solutions";

    public static readonly string NQueen_Solution_Mode =
        @" Values one of the following: 0 - 'Single', 1 - 'Unique', or 2 - 'All'";

    public static readonly string NQUEEN_BOARDSIZE =
        @$" Whole Numbers in the Range:
                [1, {BoardSettings.MaxSizeForSingle}] for 'Single',
                [1, {BoardSettings.MaxSizeForUnique}] for 'Unique',
                [1, {BoardSettings.MaxSizeForAll}] for 'All' Solutions";

    public static readonly string Bitmask_Help =
        @" bitmask - Run the high-performance bitmask N-Queens solver\n\n" +
        "You will be prompted for board size and solution mode (All, Unique, Single).\n" +
        "The solver will display the number of solutions and elapsed time.";

    private static void DumpAllHelp()
    {
        DumpHelpText(NQueen_Solution_Mode);
        DumpHelpText(NQueen_Help_Board_Size);
        DumpHelpText(Bitmask_Help);
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
        Console.WriteLine($"\tbitmask"); // Add bitmask to help output
        Console.WriteLine();
        ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "Example Command");
        Console.WriteLine($"\t{Command_Example}");
    }

    public const string Banner =
        @"
                |=============================================================|
                | NQueen.ConsoleApp - A .NET 10.0 Console Application         |
                |                                                             |
                | (c) 2025 - Ramin Anvar and Lars Erik Pedersen               |
                |                                                             |
                | App Developed for Solving N-Queen Problem                   |
                | Using the Iterative Symmetry Pruning Backtracking Algorithm |
                |                                                             |
                | Version 0.90. Use help to list available commands.          |
                |                                                             |
                |=============================================================|
        ";
}
