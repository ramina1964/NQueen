namespace NQueen.GUI.Messaging;

public class ProgressValueChangedMessage(int value)
{
    public int Value { get; } = value;
}
