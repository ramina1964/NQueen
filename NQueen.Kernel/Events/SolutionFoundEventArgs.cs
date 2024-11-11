namespace NQueen.Kernel.Events;

public class SolutionFoundEventArgs(byte[] solution) : EventArgs
{
    public byte[] Solution { get; } = solution;
}
