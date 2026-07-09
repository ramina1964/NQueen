namespace NQueen.UnitTests.Tests.Solver;

/// <summary>
/// Edge-case coverage for BitmaskSolver construction, configuration, and lifecycle:
/// the DelayInMillisec clamping rule, the constructor null-guard, the two Solve()
/// board-size guards, and Dispose() idempotency. These are pure/deterministic and do
/// not exercise the search itself (that is covered by the mode-specific suites).
/// </summary>
[Trait("Category", "Solver")]
[Trait("Mode", "Config")]
public class BitmaskSolverConfigTests
{
    private static BitmaskSolver MakeSolver() =>
        new(new SolutionFormatter()) { EnableEvents = false };

    // ── DelayInMillisec clamping ──────────────────────────────────────────────
    // Rule: value <= 0 => 0; otherwise Math.Max(MinDelayInMilliseconds, value).

    [Theory]
    [InlineData(-100)]
    [InlineData(-1)]
    [InlineData(0)]
    public void DelayInMillisec_NonPositive_ClampsToZero(int input)
    {
        using var solver = MakeSolver();
        solver.DelayInMillisec = input;
        solver.DelayInMillisec.ShouldBe(0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void DelayInMillisec_BelowMinimum_ClampsToMinimum(int input)
    {
        using var solver = MakeSolver();
        solver.DelayInMillisec = input;
        solver.DelayInMillisec.ShouldBe(SimulationSettings.MinDelayInMilliseconds);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(50)]
    [InlineData(1000)]
    public void DelayInMillisec_AtOrAboveMinimum_IsPreserved(int input)
    {
        using var solver = MakeSolver();
        solver.DelayInMillisec = input;
        solver.DelayInMillisec.ShouldBe(input);
    }

    // ── Constructor null-guard ────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullFormatter_Throws() =>
        Should.Throw<ArgumentNullException>(() => new BitmaskSolver(null!));

    // ── Solve() board-size guards ─────────────────────────────────────────────

    [Fact]
    public void Solve_BoardSizeNotSet_ThrowsInvalidOperation()
    {
        // Default BoardSize is 0 when constructed via the formatter-only overload.
        using var solver = MakeSolver();
        Should.Throw<InvalidOperationException>(() => solver.Solve());
    }

    [Fact]
    public void Solve_BoardSizeAboveBitmaskMax_ThrowsNotSupported()
    {
        using var solver = new BitmaskSolver(
            boardSize: BoardSettings.MaxBitmaskBoardSize + 1,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Hide,
            solutionFormatter: new SolutionFormatter())
        { EnableEvents = false };

        Should.Throw<NotSupportedException>(() => solver.Solve());
    }

    // ── Dispose() idempotency ─────────────────────────────────────────────────

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var solver = MakeSolver();
        solver.Dispose();
        Should.NotThrow(solver.Dispose);
    }
}
