namespace NQueen.Domain.Interfaces;

// Todo: This is used in legacy Solver, i.e., NQueen.Kernel, and should be removed.
public interface ISolverUi
{
    int DelayInMilliseconds { get; set; }

    double ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressValueChangedEventArgs> ProgressValueChanged;
}
