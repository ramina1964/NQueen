namespace NQueen.GUI.Messaging;

public class QueenPlacedMessage(Memory<int> solution, double value)
{
    public Memory<int> Solution { get; } = solution;

    public double Value { get; } = value;
}
