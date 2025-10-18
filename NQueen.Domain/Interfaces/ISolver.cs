namespace NQueen.Domain.Interfaces;

public interface ISolver: ISolverBackEnd, ISolverFrontEnd
{
    bool UseCountOnlyUniqueMode { get; set; }

    bool UseCountOnlyAllMode { get; set; }
}
