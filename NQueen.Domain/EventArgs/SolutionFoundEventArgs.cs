namespace NQueen.Domain.EventArgs;

public sealed class SolutionFoundEventArgs : System.EventArgs
{
    public Memory<int> Solution { get; }
    public int BoardSize { get; }
    public UInt128 PackedCanonical { get; }

    public SolutionFoundEventArgs(Memory<int> solution, int boardSize, UInt128 packedCanonical)
    {
        Solution = solution;
        BoardSize = boardSize;
        PackedCanonical = packedCanonical;
    }

    public SolutionFoundEventArgs(Memory<int> solution, int boardSize)
    {
        Solution = solution;
        BoardSize = boardSize;
        PackedCanonical = 0;
    }

    public SolutionFoundEventArgs(UInt128 packedCanonical, int boardSize)
    {
        Solution = Memory<int>.Empty;
        BoardSize = boardSize;
        PackedCanonical = packedCanonical;
    }
}
