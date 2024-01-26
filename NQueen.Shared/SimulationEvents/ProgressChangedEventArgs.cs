namespace NQueen.Shared.SimulationEvents;

public class ProgressValueChangedEventArgs(double value) : EventArgs
{
    public double Value { get; } = value;
}
