namespace NQueen.GUI.Messaging;

public class SolutionFoundMessage(int[] solution)
{
    public int[] Solution { get; } = solution;
}
