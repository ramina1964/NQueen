namespace NQueen.GUI.MessagePruning;

public class SolutionFoundMessage(Memory<int> solution)
{
    public Memory<int> Solution { get; } = solution;
}
