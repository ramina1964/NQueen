namespace NQueen.Shared;

public class QueenPlacedEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
