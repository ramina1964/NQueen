namespace NQueen.UnitTests.Tests.NQueenSolver;

public class SolverSymmetryTests
{
    [Fact]
    public void SymmetryHelper_GivenN5BaseSolution_GeneratesDistinctSymmetricVariants()
    {
        // Arrange
        var comparer = MemoryIntArrayComparer.Instance;
        var solutions = new HashSet<Memory<int>>(comparer);
        var baseSolution = new Memory<int>(ExpectedSolutions.N5Base);
        solutions.Add(baseSolution);

        // Act
        var symmetricalSolutions = GetSymmetricalTransformations(baseSolution.Span.ToArray())
            .ToArray();

        foreach (var symmetrical in symmetricalSolutions)
            solutions.Add(new Memory<int>(symmetrical));

        // Assert
        solutions.Count.Should().Be(ExpectedSolutions.ExpectedSymmetryVariantCountN5);

        foreach (var symmetrical in symmetricalSolutions)
        {
            var memorySolution = new Memory<int>(symmetrical);
            solutions.Contains(memorySolution).Should().BeTrue($"Symmetrical solution {string.Join(',', symmetrical)} should be detected.");
        }
    }

    private static List<int[]> GetSymmetricalTransformations(int[] solution)
    {
        int n = solution.Length;
        var scratch = new int[n * 8];
        var variants = new List<int[]>(8);
        for (int t = 0; t < 8; t++)
        {
            var buf = new int[n];
            SymmetryHelper.GetCanonicalForm(solution, scratch, buf);
            variants.Add(buf.ToArray());
        }
        return variants;
    }
}
