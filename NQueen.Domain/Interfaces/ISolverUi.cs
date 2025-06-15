namespace NQueen.Domain.Interfaces;

public interface ISolverUi
{
    int DelayInMilliseconds { get; set; }

    double ProgressValue { get; set; }
    event EventHandler<QueenPlacedEventArgs> QueenPlaced;

    event EventHandler<SolutionFoundEventArgs> SolutionFound;

    event EventHandler<ProgressValueChangedWithTokenEventArgs> ProgressValueChanged;
}
