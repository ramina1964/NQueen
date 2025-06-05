namespace NQueen.Domain.EventArgs;

public class SolutionFoundEventArgs(int[] solution) : System.EventArgs
{
    public int[] Solution { get; } = solution;
}
