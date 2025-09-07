namespace NQueen.Domain.EventArgsPruning;

public class ProgressUpdateEventArgs(
    double value, Guid simulationToken) : System.EventArgs
{
    public double Value { get; } = value;

    public Guid SimulationToken { get; } = simulationToken;
}
