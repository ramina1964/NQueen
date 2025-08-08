namespace NQueen.Domain.EventArgs;

public class ProgressValueChangedWithTokenEventArgs(
    int value, Guid simulationToken) : System.EventArgs
{
    public int Value { get; } = value;

    public Guid SimulationToken { get; } = simulationToken;
}
