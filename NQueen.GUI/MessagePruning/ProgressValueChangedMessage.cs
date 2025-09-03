namespace NQueen.GUI.MessagePruning;

public class ProgressValueChangedMessage(double value)
{
    public double Value { get; } = value;
}
