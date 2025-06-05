namespace NQueen.Domain.EventArgs;

public class ProgressValueChangedEventArgs(double value) : System.EventArgs
{
    public double Value { get; } = value;
}
