﻿namespace NQueen.Kernel.Services;

public class ResultPresentation
{
    #region Constructor
    public ResultPresentation(SimulationResults results)
    {
        MaxNoOfSolutionsInOutput = 50;
        BoardSize = results.BoardSize;
        Solutions = new ObservableCollection<Solution>(results.Solutions);
        NoOfSolutions = Solutions.Count;
        ElapsedTimeInSec = results.ElapsedTimeInSec;
        NoOfSolutionsInOutput = Math.Min(NoOfSolutions, MaxNoOfSolutionsInOutput);
    }
    #endregion Constructor

    public ObservableCollection<Solution> Solutions { get; }

    public double ElapsedTimeInSec { get; set; }

    public int NoOfSolutions { get; }

    public int MaxNoOfSolutionsInOutput { get; }

    public int BoardSize { get; set; }

    public int NoOfSolutionsInOutput { get; }

    public static StringBuilder FormatSingleSolution(Solution solution) =>
        new StringBuilder().Append($"{solution.Details}");

    public string Write2File(SolutionMode solutionMode)
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.Parent;
        var fileName = $"Size - {BoardSize}.txt";
        var subFolder = Path.Combine(dir.FullName, "Results");
        var filePath = Path.Combine(subFolder, fileName);

        var _ =
                Directory.Exists(subFolder)
                ? new DirectoryInfo(subFolder)
                : Directory.CreateDirectory(subFolder);

        File.WriteAllText(filePath, PrintFinalResults(solutionMode));
        return filePath;
    }

    public StringBuilder FormatSolutionTitle(SolutionMode solutionMode)
    {
        const string prefix = "Sol. No.";
        const int prefixLength = 8;
        const int counterPlaces = prefixLength - 28;

        var title = SolverHelper.SolutionTitle(solutionMode, NoOfSolutions);
        var sb = new StringBuilder().AppendLine(title);

        for (var index = 0; index < NoOfSolutionsInOutput; index++)
        {
            var solTitle = $"{prefix,0} {index + 1,counterPlaces}";
            sb.Append(solTitle).Append(FormatSingleSolution(Solutions[index]) + "\n");
        }

        return sb.AppendLine();
    }

    #region PrivateMembers
    private StringBuilder GetSummary(SolutionMode solutionMode)
    {
        const int placeHolderText = -28;
        const int decimalPlaces = 2;
        const int placeHolderInt = 20;

        var sb =
            new StringBuilder().AppendLine()
            .AppendLine("Summary of Results:")
            .AppendLine($"{"Date and Time",placeHolderText}{DateTime.Now,placeHolderInt}");

        sb.AppendLine($"{"BoardSize",placeHolderText}{BoardSize,placeHolderInt}");
        if (NoOfSolutions > MaxNoOfSolutionsInOutput)
        { sb.AppendLine($"No. of Solutions Included{MaxNoOfSolutionsInOutput,placeHolderInt}"); }

        // Here is: NoOfSolutions <= MaxNoOfSolutionsInOutput
        else
        {
            sb.AppendLine(solutionMode == SolutionMode.All
                  ? $"{"No. of All Solutions",placeHolderText}{NoOfSolutions,placeHolderInt}"
                  : $"{"No. of Unique Solutions",placeHolderText}{NoOfSolutions,placeHolderInt}");
        }

        var timeInSec = Math.Round(ElapsedTimeInSec, decimalPlaces);
        sb.AppendLine($"{"Elapsed Time (s)",placeHolderText}{timeInSec,placeHolderInt}");

        return sb;
    }

    private string PrintFinalResults(SolutionMode solutionMode)
    {
        var result = new StringBuilder()
                    .Append(FormatSolutionTitle(solutionMode))
                    .Append(GetSummary(solutionMode))
                    .AppendLine();

        return result.ToString();
    }
    #endregion PrivateMembers
}
