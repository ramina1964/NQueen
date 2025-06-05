namespace NQueen.Domain.EventArgs;

public class QueenPlacedEventArgs(int[] solution) : System.EventArgs
{
    public int[] Solution { get; } = solution;
}
