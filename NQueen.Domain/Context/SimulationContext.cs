namespace NQueen.Domain.Context;

public record SimulationContext(
    int BoardSize,
    SolutionMode SolutionMode,
    DisplayMode DisplayMode);
