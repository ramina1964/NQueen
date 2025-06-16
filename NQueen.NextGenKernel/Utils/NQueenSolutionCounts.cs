namespace NQueen.NextGenKernel.Utils;

public static class NQueenSolutionCounts
{
    // Unique solutions for board sizes
    public static readonly Dictionary<int, int> UniqueSolutions = new()
    {
        { 1, 1 },
        { 2, 0 },
        { 3, 0 },
        { 4, 1 },
        { 5, 2 },
        { 6, 1 },
        { 7, 6 },
        { 8, 12 },
        { 9, 46 },
        { 10, 92 },
        { 11, 341 },
        { 12, 1787 },
        { 13, 9233 },
        { 14, 45752 },
        { 15, 285053 },
        { 16, 1846955},
        { 17, 11977939},
    };

    // All solutions for board sizes
    public static readonly Dictionary<int, int> AllSolutions = new()
    {
        { 1, 1 },
        { 2, 0 },
        { 3, 0 },
        { 4, 2 },
        { 5, 10 },
        { 6, 4 },
        { 7, 40 },
        { 8, 92 },
        { 9, 352 },
        { 10, 724 },
        { 11, 2680 },
        { 12, 14200 },
        { 13, 73712 },
        { 14, 365596},
        { 15, 2279184},
        { 16, 14772512},
        { 17, 95815104},
    };
}
