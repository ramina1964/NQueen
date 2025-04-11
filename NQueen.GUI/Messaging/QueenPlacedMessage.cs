namespace NQueen.GUI.Messaging;

public class QueenPlacedMessage
{
    public int[] Solution { get; }

    public QueenPlacedMessage(int[] solution)
    {
        Solution = solution;
    }
}