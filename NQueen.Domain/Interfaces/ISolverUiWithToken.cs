namespace NQueen.Domain.Interfaces;

// Todo: Simplify ISolverUi, ISolverUiWithToken, ISolverWithToken, and ISolverBackEnd.
public interface ISolverUiWithToken
{
    int DelayInMilliseconds { get; set; }

    double ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;

    event EventHandler<SolutionFoundEventArgs> SolutionFound;

    event EventHandler<ProgressValueChangedWithTokenEventArgs> ProgressValueChanged;
}
