namespace NQueen.Domain.EventArgs;

public class SolutionFoundEventArgs(Memory<int> solution) : System.EventArgs
{
    public Memory<int> Solution { get; } = solution;
}
