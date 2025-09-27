namespace NQueen.Domain.EventArgsPruning;

public readonly struct QueenPlacedEventArgs
{
    public QueenPlacedEventArgs(Memory<int> solution)
    {
        Solution = solution;
    }
    public Memory<int> Solution { get; }
}
