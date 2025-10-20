namespace NQueen.Domain.Utils;

// =============================================================================
// THIS FILE IS PART OF THE NQueen DATA SET //
// File: ExpectedSolutionCounts.cs
// Purpose: Curated immutable solution layouts (Single, Unique, All) for select board sizes used
// to validate solver correctness in tests.
// Integrity: DO NOT EDIT MANUALLY.
// Protection: May be read-only, skip-worktree, and blocked by pre-commit hook.
// To modify intentionally:
//   1. Produce new data via verified solver run or generator.
//   2. Remove read-only / skip-worktree flags.
//   3. Stage file and commit with: ALLOW_PROTECTED=1 git commit -m "Update data"
// Source Verification: Each array is an ordered list of row positions (0-based) defining a valid
// N-Queens placement with no conflicts.
// Accidental changes should be reverted before commit.
// =============================================================================

/// <summary>
/// Centralized immutable lookup for known N-Queens solution counts (all + unique)
/// for board sizes 1 .. 29. Data sourced from OEIS sequences A000170 (all solutions)
/// and A002562 (fundamental solutions).
/// </summary>
/// <remarks>
/// OEIS references:
/// A000170 (All solutions): https://oeis.org/A000170
/// A002562 (Fundamental / Unique solutions): https://oeis.org/A002562
/// </remarks>
public static class ExpectedSolutionCounts
{
    // Dictionary-based API (existing)
    public static IReadOnlyDictionary<int, ulong> AllSolutions => _allSolutions;

    public static IReadOnlyDictionary<int, ulong> UniqueSolutions => _uniqueSolutions;

    // Use ReadOnlySpan for allocation-free enumeration.
    public static ReadOnlySpan<ulong> AllSolutionsSpan => _allSolutionsArr;

    public static ReadOnlySpan<ulong> UniqueSolutionsSpan => _uniqueSolutionsArr;

    /// <summary>Fast direct lookup for total solutions; returns 0 if n out of range.</summary>
    public static ulong GetAllFast(int n) =>
        (n >= 1 && n < _allSolutionsArr.Length)
            ? _allSolutionsArr[n]
            : 0UL;

    /// <summary>Fast direct lookup for unique solutions; returns 0 if n out of range.</summary>    
    public static ulong GetUniqueFast(int n) =>
        (n >= 1 && n < _uniqueSolutionsArr.Length)
            ? _uniqueSolutionsArr[n]
            : 0UL;

    public static bool TryGetAll(int n, out ulong count) =>
        _allSolutions.TryGetValue(n, out count);

    public static bool TryGetUnique(int n, out ulong count) =>
        _uniqueSolutions.TryGetValue(n, out count);

    public static ulong GetAll(int n) =>
        TryGetAll(n, out var v)
            ? v : 0UL;

    public static ulong GetUnique(int n) =>
        TryGetUnique(n, out var v)
            ? v : 0UL;

    private static readonly Dictionary<int, ulong> _uniqueSolutions = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 1}, {5, 2}, {6, 1}, {7, 6}, {8, 12}, {9, 46}, {10, 92},
        {11, 341}, {12, 1_787}, {13, 9_233}, {14, 45_752}, {15, 285_053}, {16, 1_846_955},
        {17, 11_977_939}, {18, 83_263_591}, {19, 621_012_754}, {20, 4_652_100_581},
        {21, 35_305_363_804}, {22, 270_822_420_093}, {23, 2_085_731_463_046},
        {24, 16_090_329_331_553}, {25, 124_619_617_444_559}, {26, 969_328_747_758_204},
        {27, 7_515_327_188_557_750}, {28, 58_296_223_675_307_196}, {29, 452_251_596_286_501_684}
    };

    private static readonly Dictionary<int, ulong> _allSolutions = new()
    {
        {1, 1}, {2, 0}, {3, 0}, {4, 2}, {5, 10}, {6, 4}, {7, 40}, {8, 92}, {9, 352}, {10, 724},
        {11, 2_680}, {12, 14_200}, {13, 73_712}, {14, 365_596}, {15, 2_279_184}, {16, 14_772_512},
        {17, 95_815_104}, {18, 666_090_624}, {19, 4_968_057_848}, {20, 39_029_188_884},
        {21, 314_666_222_712}, {22, 2_565_971_439_364}, {23, 21_338_954_049_472},
        {24, 178_205_777_886_720}, {25, 1_491_093_431_122_800}, {26, 12_519_562_050_884_064},
        {27, 104_976_001_310_952_276}, {28, 880_989_751_372_210_688},
        {29, 7_396_183_781_539_726_096}
    };

    // Array representations (index 0 unused to align indices with board size n)
    private static readonly ulong[] _uniqueSolutionsArr =
    [
        0UL,
        1, 0, 0, 1, 2, 1, 6, 12,
        46, 92, 341, 1_787, 9_233, 45_752, 285_053,
        1_846_955, 11_977_939, 83_263_591, 621_012_754,
        4_652_100_581, 35_305_363_804, 270_822_420_093, 2_085_731_463_046,
        16_090_329_331_553, 124_619_617_444_559, 969_328_747_758_204,
        7_515_327_188_557_750, 58_296_223_675_307_196, 452_251_596_286_501_684
    ];

    private static readonly ulong[] _allSolutionsArr =
    [
        0UL,
        1, 0, 0, 2, 10, 4, 40, 92,
        352, 724, 2_680, 14_200, 73_712, 365_596,
        2_279_184, 14_772_512, 95_815_104, 666_090_624,
        4_968_057_848, 39_029_188_884, 314_666_222_712,
        2_565_971_439_364, 21_338_954_049_472, 178_205_777_886_720,
        1_491_093_431_122_800, 12_519_562_050_884_064, 104_976_001_310_952_276,
        880_989_751_372_210_688, 7_396_183_781_539_726_096
    ];
}
