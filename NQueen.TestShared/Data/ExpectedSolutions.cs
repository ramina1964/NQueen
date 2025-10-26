namespace NQueen.TestShared.Data;

/// <summary>
/// Unified access point for expected solution boards and counts used across tests.
/// Wraps Domain data sources to reduce scattering of literals and magic numbers.
/// </summary>
public static class ExpectedSolutions
{
    // Counts (redirect to Domain authoritative sources)
    public static ulong GetAllCount(int n) => ExpectedSolutionCounts.GetAll(n);
    
    public static ulong GetUniqueCount(int n) => ExpectedSolutionCounts.GetUnique(n);

    // Solution boards (redirect)
    public static IReadOnlyDictionary<int, List<int[]>> Single => ExpectedSolutionData.SingleSolutions;
    
    public static IReadOnlyDictionary<int, List<int[]>> Unique => ExpectedSolutionData.UniqueSolutions;
    
    public static IReadOnlyDictionary<int, List<int[]>> All => ExpectedSolutionData.AllSolutions;

    // Packed solution boards for fast comparison
    public static IReadOnlyDictionary<int, List<UInt128>> SinglePacked => Single.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.Select(arr => Pack(arr)).ToList()
    );
    public static IReadOnlyDictionary<int, List<UInt128>> UniquePacked => Unique.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.Select(arr => Pack(arr)).ToList()
    );
    public static IReadOnlyDictionary<int, List<UInt128>> AllPacked => All.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value.Select(arr => Pack(arr)).ToList()
    );

    // Commonly reused concrete boards
    public static readonly int[] N5Base = [0, 2, 4, 1, 3];
    public static readonly int[] N5Alt = [1, 3, 0, 2, 4];
    public static readonly int[] N5BaseArray = [0, 2, 4, 1, 3];
    public static readonly int[] N5AltArray = [1, 3, 0, 2, 4];
    public static readonly int[] N5Symmetry1 = [4, 2, 0, 3, 1];
    public static readonly int[] N5Symmetry2 = [1, 3, 0, 2, 4];
    public static readonly int[] N3Base = [0, 1, 2];
    public static readonly int[] N3Alt = [2, 1, 0];

    // Packed versions for fast test comparison
    public static readonly UInt128 N5BasePacked = Pack(N5Base);
    public static readonly UInt128 N5AltPacked = Pack(N5Alt);
    public static readonly UInt128 N5Symmetry1Packed = Pack(N5Symmetry1);
    public static readonly UInt128 N5Symmetry2Packed = Pack(N5Symmetry2);
    public static readonly UInt128 N3BasePacked = Pack(N3Base);
    public static readonly UInt128 N3AltPacked = Pack(N3Alt);

    // Symmetry expectations
    public const int ExpectedSymmetryVariantCountN5 = 8; // D4 group size

    // Theory data sets (avoid duplication in individual test classes)
    public static TheoryData<int[]> N5BaseSolutions => [N5BaseArray, N5AltArray];

    public static TheoryData<UInt128> N5BaseSolutionsPacked => [N5BasePacked, N5AltPacked];

    public static TheoryData<int[], int[], bool> MemoryComparerEqualityCases => new()
    {
        { N5Symmetry1, N5Symmetry1, true },
        { N5Symmetry1, N5Symmetry2, false },
        { N3Base, N3Base, true },
        { N3Base, N3Alt, false }
    };

    public static TheoryData<UInt128, UInt128, bool> MemoryComparerEqualityCasesPacked => new()
    {
        { N5Symmetry1Packed, N5Symmetry1Packed, true },
        { N5Symmetry1Packed, N5Symmetry2Packed, false },
        { N3BasePacked, N3BasePacked, true },
        { N3BasePacked, N3AltPacked, false }
    };

    // Helper to pack int[] to UInt128
    public static UInt128 Pack(int[] arr) => NQueen.Domain.Utils.SymmetryHelper.PackRows(arr);
}
