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

    [Fact]
    public void CountSolutions_TooSmall_Throws() =>
        FluentActions.Invoking(() => BitboardNQueenSolver.CountSolutions(0))
            .Should().Throw<ArgumentOutOfRangeException>();

    [Fact]
    public void CountSolutions_TooLarge_Throws() =>
        FluentActions.Invoking(() => BitboardNQueenSolver.CountSolutions(33))
            .Should().Throw<ArgumentOutOfRangeException>();
}
