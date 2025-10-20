namespace NQueen.Domain.Utils;

/// <summary>
/// Centralized immutable lookup for known N-Queens solution counts (all + unique)
/// for board sizes 1 .. 29. Data sourced from OEIS sequences A000170 (all solutions)
/// and A002562 (fundamental solutions).
/// </summary>
public static class ExpectedSolutionCounts
{
    public static IReadOnlyDictionary<int, ulong> AllSolutions => _allSolutions;

    public static IReadOnlyDictionary<int, ulong> UniqueSolutions => _uniqueSolutions;

    public static bool TryGetAll(int n, out ulong count) =>
        _allSolutions.TryGetValue(n, out count);

    public static bool TryGetUnique(int n, out ulong count) =>
        _uniqueSolutions.TryGetValue(n, out count);

    public static ulong GetAll(int n) => TryGetAll(n, out var v) ? v : 0UL;

    public static ulong GetUnique(int n) => TryGetUnique(n, out var v) ? v : 0UL;

    private static readonly Dictionary<int, ulong> _uniqueSolutions = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 1}, {5, 2}, {6, 1}, {7, 6}, {8, 12},
        {9, 46}, {10, 92}, {11, 341}, {12, 1_787}, {13, 9_233}, {14, 45_752}, {15, 285_053},
        {16, 1_846_955}, {17, 11_977_939}, {18, 83_263_591}, {19, 621_012_754},
        {20, 4_652_100_581}, {21, 35_305_363_804}, {22, 270_822_420_093}, {23, 2_085_731_463_046},
        {24, 16_090_329_331_553}, {25, 124_619_617_444_559}, {26, 969_328_747_758_204},
        {27, 7_515_327_188_557_750}, {28, 58_296_223_675_307_196}, {29, 452_251_596_286_501_684}
    };

    private static readonly Dictionary<int, ulong> _allSolutions = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 2}, {5, 10}, {6, 4}, {7, 40}, {8, 92},
        {9, 352}, {10, 724}, {11, 2_680}, {12, 14_200}, {13, 73_712}, {14, 365_596},
        {15, 2_279_184}, {16, 14_772_512}, {17, 95_815_104}, {18, 666_090_624},
        {19, 4_968_057_848}, {20, 39_029_188_884}, {21, 314_666_222_712},
        {22, 2_565_971_439_364}, {23, 21_338_954_049_472}, {24, 178_205_777_886_720},
        {25, 1_491_093_431_122_800}, {26, 12_519_562_050_884_064}, {27, 104_976_001_310_952_276},
        {28, 880_989_751_372_210_688}, {29, 7_396_183_781_539_726_096}
    };
}
