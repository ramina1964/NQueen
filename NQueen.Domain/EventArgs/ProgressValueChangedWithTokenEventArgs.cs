namespace NQueen.Domain.EventArgs;

public class ProgressChangedWithTokenEventArgs(
    double value, Guid simulationToken) : System.EventArgs
{
    public double Value { get; } = value;

    public Guid SimulationToken { get; } = simulationToken;
}
