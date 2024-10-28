namespace NQueen.Kernel.Events;

public class QueenPlacedEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
