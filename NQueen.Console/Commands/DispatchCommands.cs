namespace NQueen.ConsoleApp.Commands;

public static class DispatchCommands
{
    public static char WhiteQueen { get; set; } = '\u2655';

    public static sbyte BoardSize { get; set; }

    public static SolutionMode SolutionMode { get; set; }

    public static bool IsSolutionModeSingle => SolutionMode == SolutionMode.Single;

    public static bool IsSolutionModeUnique => SolutionMode == SolutionMode.Unique;

    public static bool IsSolutionModeAll => SolutionMode == SolutionMode.All;

    public static Dictionary<string, bool> Commands { get; set; }

    public static Dictionary<string, string> AvailableCommands { get; set; }

    public static bool ProcessCommand(string key, string value)
    {
        var returnValue = false;
        key = key.Replace("  ", " ").TrimEnd().ToUpper();
        if (key == string.Empty)
        { Environment.Exit(0); }

        return key switch
        {
            "RUN" => RunApp().Result,
            "SOLUTIONMODE" => CheckSolutionMode(value),
            "BOARDSIZE" => CheckBoardSize(value),
            _ => returnValue,
        };
    }

    public static void ProcessCommandsInteractively()
    {
        while (Commands.All(e => e.Value) == false)
        {
            var required = GetRequiredCommand();
            if (required == "RUN")
            {
                RunSolver();
                break;
            }

            ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, $"Enter a {required} ");
            Console.WriteLine($"\t{AvailableCommands[required]}");
            var userInput = Console.ReadLine().Trim().ToLower();
            if (userInput.Equals("help") || userInput.Equals("-h"))
            {
                HelpCommands.ProcessHelpCommand(userInput);
            }
            else
            {
                var ok = DispatchCommands.ProcessCommand(required, userInput);
                if (ok)
                {
                    Commands[required] = true;
                    if (required.Trim().ToUpper() == "BOARDSIZE")
                    {
                        BoardSize = Convert.ToSByte(userInput);
                    }
                }
            }
        }
    }

    public static void ProcessCommandsFromArgs(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            (string key, string value) = ParseInput(args[i]);
            var ok = DispatchCommands.ProcessCommand(key, value);
            if (ok)
            {
                Commands[key.ToUpper()] = true;
                if (key.Equals("BOARDSIZE"))
                {
                    BoardSize = Convert.ToSByte(value);
                }
            }
        }

        if (GetRequiredCommand() == "RUN")
        {
            ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, "Solver is running:\n");
            DispatchCommands.ProcessCommand("RUN", "ok");
        }
    }

    public static void ShowErrorExit(string errorString)
    {
        ConsoleColor priorColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("ERROR: ");
        Console.ForegroundColor = priorColor;
        Console.WriteLine(errorString);
        Console.WriteLine();
        Environment.Exit(-1);
    }

    private static string GetRequiredCommand()
    {
        var cmd = Commands.Where(e => !e.Value).Select(e => e.Key).FirstOrDefault();
        return cmd ?? "";
    }

    private const string _bannerString =
                @"
                        |====================================================|
                        | NQueen.ConsoleApp - A .NET 8.0 Console Application |
                        |                                                    |
                        | (c) 2022 - Ramin Anvar and Lars Erik Pedersen      |
                        |                                                    |
                        | App Developed for Solving N-Queen Problem          |
                        | Using the Iterative Backtracking Algorithm         |
                        |                                                    |
                        | Version 0.60. Use help to list available commands. |
                        |                                                    |
                        |====================================================|
                    ";

    public static void InitCommands()
    {
        Commands = new Dictionary<string, bool>
        {
            ["SOLUTIONMODE"] = false,
            ["BOARDSIZE"] = false,
            ["RUN"] = false
        };
        AvailableCommands = new Dictionary<string, string>
        {
            ["SOLUTIONMODE"] = HelpCommands.NQUEEN_SOLUTIONMODE,
            ["BOARDSIZE"] = HelpCommands.NQUEEN_BOARDSIZE,
        };
    }

    public static void OutputBanner()
    {
        string[] bannerLines = _bannerString.Split("\r\n");
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

    public static void LaunchConsoleMonitor(string extraSourceNames = "")
    {
        if (DOTNETCOUNTERSENABLED)
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

    public static void RunSolver()
    {
        ConsoleUtils.WriteLineColored(ConsoleColor.Cyan, $"\nSolver is running ...");
        DispatchCommands.ProcessCommand("RUN", "ok");
        var runAgain = true;
        while (runAgain)
        {
            Console.WriteLine("\nRun again to debug memory usage?");
            Console.WriteLine("\tYes or No\n");
            var runAgainAnswer = Console.ReadLine().Trim().ToLower();
            if (runAgainAnswer.Equals("yes") || runAgainAnswer.Equals("y"))
            {
                Console.WriteLine();
                DispatchCommands.ProcessCommand("RUN", "ok");
            }
            else
            {
                runAgain = false;
            }
        }
    }

    public static (string feature, string value) ParseInput(string msg)
    {
        var option = msg.ToCharArray().TakeWhile(e => e != '=').ToArray();
        var n = msg[(option.Length + 1)..];
        return (new string(option), n);
    }

    // This is used for enabling dotnet-counters performance utility when you run the application
    private static readonly bool DOTNETCOUNTERSENABLED = false;

    #region PrivateMethods
    private static async Task<bool> RunApp()
    {
        ISolutionDev solutionDev = new SolutionDev();
        var solver = new BackTracking(solutionDev);

        var simulationResult = await solver.GetResultsAsync(BoardSize, SolutionMode, DisplayMode.Hide);
        var noOfSolutions = simulationResult.NoOfSolutions;
        var elapsedTime = simulationResult.ElapsedTimeInSec;
        if (noOfSolutions == 0)
        {
            ConsoleUtils.WriteLineColored(ConsoleColor.Blue, $"\n{Utility.NoSolutionMessage}");
            return true;
        }

        var simTitle = $"Summary of the Results for BoardSize = { BoardSize } and DisplayMode = { DispatchCommands.SolutionMode }";
        ConsoleUtils.WriteLineColored(ConsoleColor.Blue, $"\n{simTitle}:");

        ConsoleUtils.WriteLineColored(ConsoleColor.Gray, $"Number of solutions found: {noOfSolutions,10}");
        ConsoleUtils.WriteLineColored(ConsoleColor.Gray, $"Elapsed time in seconds: {elapsedTime,14}");

        var example = simulationResult.Solutions.FirstOrDefault();
        var solutionTitle = (example == null)
                            ? "\nNo Solution Found!"
                            : "\nFirst Solution Found - Numbers in paranteses: Column No. and Row No., Starting from the Lower Left Corner:";
        ConsoleUtils.WriteLineColored(ConsoleColor.Blue, solutionTitle);
        ConsoleUtils.WriteLineColored(ConsoleColor.Yellow, example.Details);
        var board = CreateChessBoard(example.QueenList);
        ConsoleUtils.WriteLineColored(ConsoleColor.Blue, $"\nDrawing of first solution:\n");

        var message = "\tIMPORTANT - You need to set default fonts (in this console window) to SimSun-ExtB in order to show unicode characters.\n";
        ConsoleUtils.WriteLineColored(ConsoleColor.Gray, message);
        Console.WriteLine(board);
        
        return true;
    }

    private static bool CheckSolutionMode(string value)
    {
        var isValid = int.TryParse(value, out int userChoice);
        if (!isValid)
        {
            Console.WriteLine("Invalid Integer. Try again.\n");
            return false;
        }

        switch (userChoice)
        {
            case 0:
                SolutionMode = SolutionMode.Single;
                return true;

            case 1:
                SolutionMode = SolutionMode.Unique;
                return true;

            case 2:
                SolutionMode = SolutionMode.All;
                return true;

            default:
                Console.WriteLine("Invalid Option: Try 0, 1, or 2.");
                return false;
        }
    }

    private static bool CheckBoardSize(string value)
    {
        var isValidNo = sbyte.TryParse(value, out sbyte size);
        if (!isValidNo)
        {
            Console.WriteLine("Invalid number. Try again.");
            return false;
        }

        BoardSize = Convert.ToSByte(size);
        if (BoardSize < 1)
        {
            Console.WriteLine("BoardSize must be a positive number.");
            return false;
        }

        if (IsSolutionModeSingle && BoardSize > Utility.MaxBoardSizeForSingleSolution)
        {
            Console.WriteLine(Utility.SizeTooLargeForSingleSolutionMsg);
            return false;
        }

        if (IsSolutionModeUnique && BoardSize > Utility.MaxBoardSizeForUniqueSolutions)
        {
            Console.WriteLine(Utility.SizeTooLargeForUniqueSolutionsMsg);
            return false;
        }

        if (IsSolutionModeAll && BoardSize > Utility.MaxBoardSizeForAllSolutions)
        {
            Console.WriteLine(Utility.SizeTooLargeForAllSolutionsMsg);
            return false;
        }

        return true;
    }

    private static string[,] ChessBoardHelper(sbyte[] queens)
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
                    arr[row, col] = col == size - 1 ? $"|{WhiteQueen}|" : $"|{WhiteQueen}"; ;
                }
                else
                {
                    arr[row, col] = col == size - 1 ? "|-|" : "|-";
                }
            }
        }

        return arr;
    }

    private static string CreateChessBoard(sbyte[] queens)
    {
        var arr = ChessBoardHelper(queens);
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

    #endregion PrivateMethods

}
