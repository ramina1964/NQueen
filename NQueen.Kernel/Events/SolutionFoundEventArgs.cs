namespace NQueen.Kernel.Events;

public class SolutionFoundEventArgs(int[] solution) : EventArgs
{
    public int[] Solution { get; } = solution;
}
