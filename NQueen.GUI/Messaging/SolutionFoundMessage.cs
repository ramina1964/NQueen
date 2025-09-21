namespace NQueen.GUI.Messaging;

public class SolutionFoundMessage(Memory<int> solution)
{
    public Memory<int> Solution { get; } = solution;
}
