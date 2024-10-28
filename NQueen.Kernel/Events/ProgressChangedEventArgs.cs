namespace NQueen.Kernel.Events;

public class ProgressValueChangedEventArgs(double value) : EventArgs
{
    public double Value { get; } = value;
}
