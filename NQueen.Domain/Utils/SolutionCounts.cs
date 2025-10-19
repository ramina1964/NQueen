namespace NQueen.Domain.Utils;

/// <summary>
/// Centralized immutable lookup for known N-Queens solution counts (all + unique) for board sizes 1..29.
/// Data sourced from OEIS sequences A000170 (all solutions) and A002562 (fundamental solutions).
/// </summary>
public static class SolutionCounts
{
    public static IReadOnlyDictionary<int, ulong> AllSolutions => _allSolutions;
    public static IReadOnlyDictionary<int, ulong> UniqueSolutions => _uniqueSolutions;

    public static bool TryGetAll(int n, out ulong count) => _allSolutions.TryGetValue(n, out count);
    public static bool TryGetUnique(int n, out ulong count) => _uniqueSolutions.TryGetValue(n, out count);

    public static ulong GetAll(int n) => TryGetAll(n, out var v) ? v : 0UL;
    public static ulong GetUnique(int n) => TryGetUnique(n, out var v) ? v : 0UL;

    private static readonly Dictionary<int, ulong> _uniqueSolutions = new()
    {
        {1,1},{2,0},{3,0},{4,1},{5,2},{6,1},{7,6},{8,12},{9,46},{10,92},{11,341},{12,1787},{13,9233},{14,45752},{15,285053},{16,1846955},{17,11977939},{18,83263591},{19,621012754},{20,4652100581},{21,35305363804},{22,270822420093},{23,2085731463046},{24,16090329331553},{25,124619617444559},{26,969328747758204},{27,7515327188557750},{28,58296223675307196},{29,452251596286501684}
    };

    private static readonly Dictionary<int, ulong> _allSolutions = new()
    {
        {1,1},{2,0},{3,0},{4,2},{5,10},{6,4},{7,40},{8,92},{9,352},{10,724},{11,2680},{12,14200},{13,73712},{14,365596},{15,2279184},{16,14772512},{17,95815104},{18,666090624},{19,4968057848},{20,39029188884},{21,314666222712},{22,2565971439364},{23,21338954049472},{24,178205777886720},{25,1491093431122800},{26,12519562050884064},{27,104976001310952276},{28,880989751372210688},{29,7396183781539726096}
    };
}
