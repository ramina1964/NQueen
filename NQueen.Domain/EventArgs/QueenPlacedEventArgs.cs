namespace NQueen.Domain.EventArgs;

public readonly struct QueenPlacedEventArgs(Memory<int> solution)
{
    public Memory<int> Solution { get; } = solution;
}
