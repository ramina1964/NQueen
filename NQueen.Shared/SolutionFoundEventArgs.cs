namespace NQueen.Shared;

public class SolutionFoundEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
