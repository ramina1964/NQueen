namespace NQueen.GUI.ViewModels;

public class ProgressValueChangedMessage(double value) : ValueChangedMessage<double>(value)
{}

public class QueenPlacedMessage(int[] solution) : ValueChangedMessage<int[]>(solution)
{}

public class SolutionFoundMessage(int[] solution) : ValueChangedMessage<int[]>(solution)
{}
