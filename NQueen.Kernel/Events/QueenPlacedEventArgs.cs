namespace NQueen.Kernel.Events;

public class QueenPlacedEventArgs(byte[] solution) : EventArgs
{
    public byte[] Solution { get; } = solution;
}
