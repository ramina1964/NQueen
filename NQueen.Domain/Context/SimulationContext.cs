namespace NQueen.Domain.Context;

public record SimulationContext(
    int BoardSize,
    SolutionMode SolutionMode,
    DisplayMode DisplayMode,
    IProgress<ProgressInfo>? OnProgress = null,
    CancellationToken Cancellation = default,
    IProgress<SolutionFoundInfo>? OnSolutionFound = null,
    ChannelWriter<QueenPlacedInfo>? OnQueenPlaced = null);
