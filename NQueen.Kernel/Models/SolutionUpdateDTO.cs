namespace NQueen.Kernel.Models;

public class SolutionUpdateDTO
{
    public int BoardSize { get; set; }

    public SolutionMode SolutionMode { get; set; }

    public int[] QueenPositions { get; set; }

    public HashSet<int[]> Solutions { get; set; }

    public int NoOfSolution => Solutions.Count;
}
