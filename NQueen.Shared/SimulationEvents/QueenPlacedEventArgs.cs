namespace NQueen.Shared.SimulationEvents;

public class QueenPlacedEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
