namespace NQueen.GUI.Messaging;

public class ProgressValueChangedMessage
{
    public ProgressValueChangedMessage(double value)
    {
        Value = value;
    }

    public double Value { get; }
}
