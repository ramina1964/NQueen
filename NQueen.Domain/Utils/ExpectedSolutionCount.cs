namespace NQueen.Domain.Utils;

public static class ExpectedSolutionCount
{
    public static int GetCount(int boardSize, SolutionMode solutionMode) =>
        solutionMode switch
        {
            SolutionMode.Unique => _uniqueSols.TryGetValue(boardSize, out var uniqueCount)
                ? uniqueCount
                : 0,

            SolutionMode.All => _allSols.TryGetValue(boardSize, out var allCount)
                ? allCount
                : 0,

            _ => 0
        };

    // Unique no of solutions
    private static readonly Dictionary<int, int> _uniqueSols = new()
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

    // All no of solutions
    private static readonly Dictionary<int, int> _allSols = new()
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
