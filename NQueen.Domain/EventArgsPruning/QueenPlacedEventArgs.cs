namespace NQueen.Domain.EventArgsPruning;

public readonly struct QueenPlacedEventArgs(Memory<int> solution)
{
    public Memory<int> Solution { get; } = solution;
}
