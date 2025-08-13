namespace NQueen.Domain.Interfaces;

public interface ISolverFrontEnd
{
    int DelayInMillisec { get; set; }

    double ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressChangedWithTokenEventArgs> ProgressValueChanged;

    void SetSimulationToken(Guid token);
}
