namespace NQueen.Kernel.Events;

public class SolutionFoundEventArgs(sbyte[] solution) : EventArgs
{
    public sbyte[] Solution { get; } = solution;
}
