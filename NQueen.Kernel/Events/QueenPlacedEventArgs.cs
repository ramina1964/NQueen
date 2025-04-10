namespace NQueen.Kernel.Events;

public class QueenPlacedEventArgs(int[] solution) : EventArgs
{
    public int[] Solution { get; } = solution;
}
