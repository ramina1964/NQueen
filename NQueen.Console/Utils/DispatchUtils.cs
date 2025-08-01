namespace NQueen.ConsoleApp.Utils;

public static class DispatchUtils
{
    public static char WhiteQueen { get; } = BoardSettings.WhiteQueenChar;

    // This enables dotnet-counters performance utility when running the application.
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

    // Todo: Change the order of indices here.
    public static string CreateChessBoard(int[] queens)
    {
        var arr = ChessBoardHelper(queens, WhiteQueen);
        var size = queens.Length;
        var board = string.Empty;
        for (var rowIndex = size - 1; rowIndex >= 0; rowIndex--)
        {
            for (var colIndex = 0; colIndex < size; colIndex++)
                board += arr[rowIndex, colIndex];

            board += Environment.NewLine;
        }

        return board;
    }

    private static string[,] ChessBoardHelper(int[] queens, char whiteQueen)
    {
        var size = queens.Length;
        string[,] arr = new string[size, size];

        for (var colIndex = 0; colIndex < size; colIndex++)
        {
            var rowIndex = queens[colIndex];
            for (var rowCounter = 0; rowCounter < size; rowCounter++)
            {
                if (rowCounter == rowIndex)
                {
                    arr[rowCounter, colIndex] = colIndex == size - 1 ? $"|{whiteQueen}|" : $"|{whiteQueen}";
                }
                else
                {
                    arr[rowCounter, colIndex] = colIndex == size - 1 ? "|-|" : "|-";
                }
            }
        }

        return arr;
    }
}
