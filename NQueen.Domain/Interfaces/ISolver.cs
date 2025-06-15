namespace NQueen.Domain.Interfaces;

public interface ISolver
{
    // Backend
    bool IsSolverCanceled { get; set; }

    Task<SimulationResults> GetResultsForBoardAsync(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode = DisplayMode.Hide);

    // UI
    int DelayInMilliseconds { get; set; }

    double ProgressValue { get; set; }

    // Events
    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressValueChangedWithTokenEventArgs> ProgressValueChanged;
}
