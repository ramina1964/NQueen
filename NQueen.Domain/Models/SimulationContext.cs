namespace NQueen.Domain.Models;

public record SimulationContext(
    int BoardSize, SolutionMode SolutionMode, DisplayMode DisplayMode = DisplayMode.Hide);