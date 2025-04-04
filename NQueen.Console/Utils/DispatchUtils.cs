namespace NQueen.ConsoleApp.Utils;

public static class DispatchUtils
{
    public static char WhiteQueen { get; } = '\u2655';

    // This is used for enabling dotnet-counters performance utility when you run the application.
    public static readonly bool DotNetCountersEnabled = false;

    public static void LaunchConsoleMonitor(string extraSourceNames = "")
    {
        if (DotNetCountersEnabled)
        {
            int processID = Environment.ProcessId;
            ProcessStartInfo ps = new()
            {
                FileName = "dotnet-counters",
                Arguments = $"monitor --process-id {processID} NQueen.ConsoleApp System.Runtime " + extraSourceNames,
                UseShellExecute = true
            };
            Process.Start(ps);
        }
    }

    public static (string feature, string value) ParseInput(string msg)
    {
        var option = msg.ToCharArray().TakeWhile(e => e != '=').ToArray();
        var n = msg[(option.Length + 1)..];

        return (new string(option), n);
    }

    public static string CreateChessBoard(int[] queens)
    {
        var arr = ChessBoardHelper(queens, WhiteQueen);
        var size = queens.Length;
        var board = string.Empty;
        for (int row = size - 1; row >= 0; row--)
        {
            for (int col = 0; col < size; col++)
            {
                board += arr[row, col];
            }
            board += Environment.NewLine;
        }

        return board;
    }

    public static (bool isValid, int boardSize) CheckBoardSize(
        string value, SolutionMode solutionMode)
    {
        if (int.TryParse(value, out int size) == false)
        {
<<<<<<< HEAD
            HelpCommands.ShowExitError(Messages.AllSizeOutOfRangeMsg);
=======
            HelpCommands.ShowExitError(CommandConst.SizeTooLargeForAllSolutions);
>>>>>>> d55b1a3bd4e45f249bd14aefeeae9613b65a0525
            return (false, 0);
        }

        if (size < 1)
        {
            HelpCommands.ShowExitError("BoardSize must be a positive number.");
            return (false, 0);
        }

        if (solutionMode == SolutionMode.Single && size > BoardSettings.MaxBoardSizeInSingleSolution)
        {
            HelpCommands.ShowExitError(Messages.SingleSizeOutOfRangeMsg);
            return (false, 0);
        }

        if (solutionMode == SolutionMode.Unique && size > BoardSettings.MaxBoardSizeInUniqueSolutions)
        {
            HelpCommands.ShowExitError(Messages.UniqueSizeOutOfRangeMsg);
            return (false, 0);
        }

        if (solutionMode == SolutionMode.All && size > BoardSettings.MaxBoardSizeInAllSolutions)
        {
            HelpCommands.ShowExitError(Messages.AllSizeOutOfRangeMsg);
            return (false, 0);
        }

        return (true, size);
    }

    private static string[,] ChessBoardHelper(int[] queens, char whiteQueen)
    {
        var size = queens.Length;
        string[,] array = new string[size, size];

        for (var col = 0; col < size; col++)
        {
            var rowPlace = queens[col];
            for (var row = 0; row < size; row++)
            {
                array[row, col] = row == rowPlace ? col == size - 1
                    ? $"|{whiteQueen}|"
                    : $"|{whiteQueen}" : col == size - 1 ? "|-|" : "|-";
            }
        }

        return array;
    }
}
