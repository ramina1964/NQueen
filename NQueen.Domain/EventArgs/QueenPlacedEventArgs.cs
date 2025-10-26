namespace NQueen.Domain.EventArgs;

public readonly struct QueenPlacedEventArgs
{
    public Memory<int> Solution { get; }
    public int BoardSize { get; }
    public UInt128 PackedCanonical { get; }
    // Legacy compatibility property
    public UInt128 PackedSolution => PackedCanonical;

    public QueenPlacedEventArgs(Memory<int> solution, int boardSize, UInt128 packedCanonical)
    {
        Solution = solution;
        BoardSize = boardSize;
        PackedCanonical = packedCanonical;
    }

    public QueenPlacedEventArgs(Memory<int> solution, int boardSize)
    {
        Solution = solution;
        BoardSize = boardSize;
        PackedCanonical = 0;
    }

    public QueenPlacedEventArgs(UInt128 packedCanonical, int boardSize)
    {
        Solution = Memory<int>.Empty;
        BoardSize = boardSize;
        PackedCanonical = packedCanonical;
    }
}
