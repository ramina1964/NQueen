namespace NQueen.Kernel.Models;

public class SolutionUpdateDTO
{
    public byte BoardSize { get; set; }

    public SolutionMode SolutionMode { get; set; }

    public byte[] QueenPositions { get; set; }

    public HashSet<byte[]> Solutions { get; set; }

    public int NoOfSolution => Solutions.Count;
}
