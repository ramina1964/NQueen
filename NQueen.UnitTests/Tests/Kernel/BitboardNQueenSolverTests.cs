namespace NQueen.UnitTests.Tests.Kernel;

public class BitboardNQueenSolverTests
{
    // ── Known solution counts (N ≤ 8 to stay fast) ──────────────────────────

    [Theory]
    [InlineData(1,  1L)]
    [InlineData(2,  0L)]
    [InlineData(3,  0L)]
    [InlineData(4,  2L)]
    [InlineData(5,  10L)]
    [InlineData(6,  4L)]
    [InlineData(7,  40L)]
    [InlineData(8,  92L)]
    public void CountSolutions_Parallel_ReturnsKnownCount(int n, long expected) =>
        BitboardNQueenSolver.CountSolutions(n, parallel: true).Should().Be(expected);

    [Theory]
    [InlineData(1,  1L)]
    [InlineData(4,  2L)]
    [InlineData(5,  10L)]
    [InlineData(8,  92L)]
    public void CountSolutions_Sequential_ReturnsKnownCount(int n, long expected) =>
        BitboardNQueenSolver.CountSolutions(n, parallel: false).Should().Be(expected);

    [Fact]
    public void CountSolutions_ParallelAndSequential_AgreeForAllSmallN()
    {
        for (int n = 1; n <= 8; n++)
        {
            var par = BitboardNQueenSolver.CountSolutions(n, parallel: true);
            var seq = BitboardNQueenSolver.CountSolutions(n, parallel: false);
            par.Should().Be(seq, $"parallel and sequential should agree for N={n}");
        }
    }

    // ── Odd-N middle-column path ─────────────────────────────────────────────

    [Theory]
    [InlineData(5,  10L)]
    [InlineData(7,  40L)]
    public void CountSolutions_OddN_IncludesMiddleColumnPath(int n, long expected) =>
        BitboardNQueenSolver.CountSolutions(n, parallel: false).Should().Be(expected);

    // ── Boundary & argument validation ──────────────────────────────────────

    [Fact]
    public void CountSolutions_N1_Returns1() =>
        BitboardNQueenSolver.CountSolutions(1).Should().Be(1L);

    [Theory]
    [InlineData(0)]   // below the minimum board size
    [InlineData(33)]  // above the maximum supported board size
    public void CountSolutions_OutOfRange_Throws(int n) =>
        FluentActions.Invoking(() => BitboardNQueenSolver.CountSolutions(n))
            .Should().Throw<ArgumentOutOfRangeException>();

    // ── Iterative variant parity (perf/all-mode-iterative-core A/B) ──────────
    // CountSolutions is the production iterative variant; CountSolutionsRecursive is the
    // recursive baseline retained internally only as the comparison cell for
    // AllCountOnlyRecursiveVsIterativeBenchmark. These tests gate count-equivalence at every
    // parallel/sequential combination across the sizes the public solver suite already covers
    // exactly. Coverage of larger N (15-18) happens via the benchmark workload itself, which
    // asserts oracle counts in its body.

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    public void CountSolutions_Parallel_MatchesRecursive(int n)
    {
        var recursive = BitboardNQueenSolver.CountSolutionsRecursive(n, parallel: true);
        var iterative = BitboardNQueenSolver.CountSolutions(n, parallel: true);
        iterative.Should().Be(recursive,
            $"the iterative production variant must agree with the recursive baseline at N={n} (parallel)");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(11)]
    [InlineData(13)]
    public void CountSolutions_Sequential_MatchesRecursive(int n)
    {
        var recursive = BitboardNQueenSolver.CountSolutionsRecursive(n, parallel: false);
        var iterative = BitboardNQueenSolver.CountSolutions(n, parallel: false);
        iterative.Should().Be(recursive,
            $"the iterative production variant must agree with the recursive baseline at N={n} (sequential)");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void CountSolutionsRecursive_OutOfRange_Throws(int n) =>
        FluentActions.Invoking(() => BitboardNQueenSolver.CountSolutionsRecursive(n))
            .Should().Throw<ArgumentOutOfRangeException>();
}
