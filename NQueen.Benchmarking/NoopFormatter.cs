namespace NQueen.Benchmarking;

/// <summary>
/// Minimal <see cref="ISolutionFormatter"/> for benchmarks that only measure counts,
/// not solution formatting.
/// </summary>
internal sealed class NoopFormatter : ISolutionFormatter
{
    public string FormatSolutions(
        IReadOnlyList<Position> queenPositions,
        IndexingType indexingType,
        int boardSize) => string.Empty;
}
