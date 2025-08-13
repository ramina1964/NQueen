namespace NQueen.GUI.Messaging;

public class ProgressValueChangedMessage(double value)
{
    public double Value { get; } = value;
}
