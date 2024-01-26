namespace NQueen.Shared.SimulationEvents;

public class SolutionFoundEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
