namespace NQueen.Domain.Interfaces;

// Todo: Add a new interface like this one, but using classes in EventArgsPruning as EventArgs:
// - QueenPlacedEventArgs, SolutionFoundEventArgs, ProgressChangedWithTokenEventArgs
// Then use the new interface in SolverEngine, instead of ISolverFrontEnd.
// Remember also to register the new core classes in the project's DI container.
// The new EventArg classes use Memory<int>, instead of int[], for improved performance.
public interface ISolverFrontEnd
{
    int DelayInMillisec { get; set; }

    int ProgressValue { get; set; }

    event EventHandler<QueenPlacedEventArgs> QueenPlaced;
    event EventHandler<SolutionFoundEventArgs> SolutionFound;
    event EventHandler<ProgressUpdateEventArgs> ProgressValueChanged;

    void SetSimulationToken(Guid token);
}
