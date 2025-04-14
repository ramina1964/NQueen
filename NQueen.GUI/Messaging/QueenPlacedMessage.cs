namespace NQueen.GUI.Messaging;

public class QueenPlacedMessage(int[] solution, double value)
{
    public int[] Solution { get; } = solution;

    public double Value { get; } = value;
}
