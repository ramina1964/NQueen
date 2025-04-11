namespace NQueen.GUI.Messaging;

public class ProgressValueChangedMessage
{
    public double Value { get; }

    public ProgressValueChangedMessage(double value)
    {
        Value = value;
    }
}
