namespace NQueen.Domain.EventArgs;

// Todo: This class is used in a legacy project and should be removed.
public class ProgressValueChangedEventArgs(double value) : System.EventArgs
{
    public double Value { get; } = value;
}
