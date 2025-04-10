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
            HelpCommands.ShowExitError(CommandConst.InvalidBoardSize);
            return (false, 0);
        }

        if (size < 1)
        {
            HelpCommands.ShowExitError("BoardSize must be a positive number.");
            return (false, 0);
        }

        if (solutionMode == SolutionMode.Single && size > Utility.MaxBoardSizeForSingleSolution)
        {
            HelpCommands.ShowExitError(Utility.SizeTooLargeForSingleSolutionMsg);
            return (false, 0);
        }

        if (solutionMode == SolutionMode.Unique && size > Utility.MaxBoardSizeForUniqueSolutions)
        {
            HelpCommands.ShowExitError(Utility.SizeTooLargeForUniqueSolutionsMsg);
            return (false, 0);
        }

        if (solutionMode == SolutionMode.All && size > Utility.MaxBoardSizeForAllSolutions)
        {
            HelpCommands.ShowExitError(Utility.SizeTooLargeForAllSolutionsMsg);
            return (false, 0);
        }

        return (true, size);
    }

    private static string[,] ChessBoardHelper(int[] queens, char whiteQueen)
    {
        var size = queens.Length;
        string[,] arr = new string[size, size];

        for (int col = 0; col < size; col++)
        {
            var rowPlace = queens[col];
            for (int row = 0; row < size; row++)
            {
                if (row == rowPlace)
                {
                    arr[row, col] = col == size - 1 ? $"|{whiteQueen}|" : $"|{whiteQueen}";
                }
                else
                {
                    arr[row, col] = col == size - 1 ? "|-|" : "|-";
                }
            }
        }

        return arr;
    }
}
