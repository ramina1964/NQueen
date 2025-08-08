namespace NQueen.Domain.Interfaces;

public interface ISolverFrontEnd
{
    int DelayInMilliseconds { get; set; }

    int ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressValueChangedWithTokenEventArgs> ProgressValueChanged;

    void SetSimulationToken(Guid token);
}
