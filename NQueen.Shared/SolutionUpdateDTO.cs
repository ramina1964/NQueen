namespace NQueen.Shared;

public class SolutionUpdateDTO
{
    public sbyte BoardSize { get; set; }

    public SolutionMode SolutionMode { get; set; }

    public sbyte[] QueenList { get; set; }

    public HashSet<sbyte[]> Solutions { get; set; }

    public int NoOfSolution => Solutions.Count;
}
