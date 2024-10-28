using NQueen.Kernel.Events;

namespace NQueen.Kernel.Interfaces;

public interface ISolverUI
{
    int DelayInMilliseconds { get; set; }

    double ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressValueChangedEventArgs> ProgressValueChanged;
}
