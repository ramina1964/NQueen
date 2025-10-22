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

    // Commonly reused concrete boards
    public static readonly int[] N5Base = [0, 2, 4, 1, 3];
    public static readonly int[] N5Alt = [1, 3, 0, 2, 4];
    public static readonly int[] N5BaseArray = [0, 2, 4, 1, 3];
    public static readonly int[] N5AltArray = [1, 3, 0, 2, 4];
    public static readonly int[] N5Symmetry1 = [4, 2, 0, 3, 1];
    public static readonly int[] N5Symmetry2 = [1, 3, 0, 2, 4];
    public static readonly int[] N3Base = [0, 1, 2];
    public static readonly int[] N3Alt = [2, 1, 0];

    // Symmetry expectations
    public const int ExpectedSymmetryVariantCountN5 = 8; // D4 group size

    // Theory data sets (avoid duplication in individual test classes)
    public static TheoryData<int[]> N5BaseSolutions => [N5BaseArray, N5AltArray];

    public static TheoryData<int[], int[], bool> MemoryComparerEqualityCases => new()
    {
        { N5Symmetry1, N5Symmetry1, true },
        { N5Symmetry1, N5Symmetry2, false },
        { N3Base, N3Base, true },
        { N3Base, N3Alt, false }
    };
}
