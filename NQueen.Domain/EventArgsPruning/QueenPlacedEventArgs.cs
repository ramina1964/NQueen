namespace NQueen.Domain.EventArgsPruning;

public class QueenPlacedEventArgs(Memory<int> solution) : System.EventArgs
{
    public Memory<int> Solution { get; } = solution;
}
