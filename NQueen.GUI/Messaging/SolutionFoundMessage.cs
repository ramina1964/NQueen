namespace NQueen.GUI.Messaging;

public class SolutionFoundMessage
{
    public int[] Solution { get; }

    public SolutionFoundMessage(int[] solution)
    {
        Solution = solution;
    }
}
