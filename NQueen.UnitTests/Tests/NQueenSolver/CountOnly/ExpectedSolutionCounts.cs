namespace NQueen.UnitTests.Tests.NQueenSolver.CountOnly;

/// <summary>
/// Known solution counts (All & Unique) for small/mid board sizes.
/// Source: standard N-Queens sequence data.
/// </summary>
internal static class ExpectedSolutionCounts
{
    public static readonly Dictionary<int, ulong> AllCounts = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 2}, {5, 10}, {6, 4}, {7, 40}, {8, 92},
        {9, 352}, {10, 724}, {11, 2680}, {12, 14200}, {13, 73712}, {14, 365596}
    };

    public static readonly Dictionary<int, ulong> UniqueCounts = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 1}, {5, 2}, {6, 1}, {7, 6}, {8, 12},
        {9, 46}, {10, 92}, {11, 341}, {12, 1787}, {13, 9233}, {14, 45752}
    };
}
