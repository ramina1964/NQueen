namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Counts")]
public class SolverSolutionCountTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    // Board sizes where we materialize (no count-only flags) for Unique & All
    public static TheoryData<int, SolutionMode> SmallBoardsCountModes => new()
    {
        {4, SolutionMode.Unique}, {4, SolutionMode.All},
        {5, SolutionMode.Unique}, {5, SolutionMode.All},
        {6, SolutionMode.Unique}, {6, SolutionMode.All},
        {7, SolutionMode.Unique}, {7, SolutionMode.All},
        {8, SolutionMode.Unique}, {8, SolutionMode.All}
    };

    // Larger boards where we run in count-only mode to avoid materialization overhead
    public static TheoryData<int, SolutionMode> LargeBoardsCountModes => new()
    {
        {9, SolutionMode.Unique}, {9, SolutionMode.All},
        {10, SolutionMode.Unique}, {10, SolutionMode.All},
        {11, SolutionMode.Unique}, {11, SolutionMode.All},
        {12, SolutionMode.Unique}, {12, SolutionMode.All},
        {13, SolutionMode.Unique}, {13, SolutionMode.All}
    };

    public static TheoryData<int, SolutionMode> MaterializeCapBoards => new()
    {
        {4, SolutionMode.Unique}, {4, SolutionMode.All},
        {5, SolutionMode.Unique}, {5, SolutionMode.All},
        {6, SolutionMode.Unique}, {6, SolutionMode.All},
        {7, SolutionMode.Unique}, {7, SolutionMode.All},
        {8, SolutionMode.Unique}, {8, SolutionMode.All},
        {9, SolutionMode.Unique}, {9, SolutionMode.All},
        {10, SolutionMode.Unique}, {10, SolutionMode.All},
        {12, SolutionMode.Unique}, {12, SolutionMode.All}
    };

    [Theory]
    [MemberData(nameof(SmallBoardsCountModes))]
    public async Task GetSimResults_SmallBoards_CountMatchesExpected(int n, SolutionMode mode)
    {
        // Capture original flags to restore after test to prevent leakage between tests using shared fixture.
        bool origAll = _solver.UseCountOnlyAllMode;
        bool origUnique = _solver.UseCountOnlyUniqueMode;
        try
        {
            _solver.UseCountOnlyUniqueMode = false;
            _solver.UseCountOnlyAllMode = false;
            var ctx = new SimulationContext(n, mode, DisplayMode.Hide);
            var results = await _solver.GetSimResultsAsync(ctx);
            ulong expected = mode switch
            {
                SolutionMode.Unique => ExpectedSolutions.GetUniqueCount(n),
                SolutionMode.All => ExpectedSolutions.GetAllCount(n),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
            results.SolutionsCount.Should().Be(expected, $"{mode} solutions count for N={n} should match expected source.");
            // Materialized solutions may be capped; assert size constraints and board size integrity
            results.Solutions.Should().NotBeNull();
            foreach (var s in results.Solutions)
                s.BoardSize.Should().Be(n);
            results.Solutions.Count.Should().BeLessThanOrEqualTo((int)expected);
        }
        finally
        {
            _solver.UseCountOnlyAllMode = origAll;
            _solver.UseCountOnlyUniqueMode = origUnique;
        }
    }

    [Theory]
    [MemberData(nameof(LargeBoardsCountModes))]
    public async Task GetSimResults_LargeBoards_CountOnly_CountMatchesExpected(int n, SolutionMode mode)
    {
        bool origAll = _solver.UseCountOnlyAllMode;
        bool origUnique = _solver.UseCountOnlyUniqueMode;
        try
        {
            // Enable appropriate count-only flag
            _solver.UseCountOnlyUniqueMode = mode == SolutionMode.Unique;
            _solver.UseCountOnlyAllMode = mode == SolutionMode.All;
            var ctx = new SimulationContext(n, mode, DisplayMode.Hide);
            var results = await _solver.GetSimResultsAsync(ctx);
            ulong expected = mode switch
            {
                SolutionMode.Unique => ExpectedSolutions.GetUniqueCount(n),
                SolutionMode.All => ExpectedSolutions.GetAllCount(n),
                _ => throw new ArgumentOutOfRangeException(nameof(mode))
            };
            results.Solutions.Should().BeEmpty($"Count-only mode should not materialize solutions for {mode} N={n}.");
            results.SolutionsCount.Should().Be(expected, $"{mode} count mismatch for N={n}");
        }
        finally
        {
            _solver.UseCountOnlyAllMode = origAll;
            _solver.UseCountOnlyUniqueMode = origUnique;
        }
    }

    [Theory]
    [MemberData(nameof(MaterializeCapBoards))]
    public async Task GetSimResults_MaterializeMode_ProducesExpectedNumberOfSolutions(
        int n, SolutionMode mode)
    {
        var formatter = new SolutionFormatter();
        var solver = new BitmaskSolver(n, mode, DisplayMode.Hide, formatter)
        {
            UseCountOnlyUniqueMode = false,
            UseCountOnlyAllMode = false,
            EnableEvents = false
        };
        var ctx = new SimulationContext(n, mode, DisplayMode.Hide);
        var results = await Task.Run(() => solver.Solve());
        ulong expected = mode switch
        {
            SolutionMode.Unique => ExpectedSolutions.GetUniqueCount(n),
            SolutionMode.All => ExpectedSolutions.GetAllCount(n),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        int expectedMaterialized = (int)Math.Min(_maxDsipayedCount, expected);
        results.Solutions.Should().NotBeNull();
        results.Solutions.Count.Should().Be(expectedMaterialized, $"Materialize mode should produce min(cap, expected) solutions for {mode} N={n}.");
        results.SolutionsCount.Should().Be(expected, $"Total solutions count for {mode} N={n} should match expected.");
        foreach (var s in results.Solutions)
            s.BoardSize.Should().Be(n);
    }

    [Theory]
    [MemberData(nameof(MaterializeCapBoards))]
    public async Task GetSimResults_MaterializeMode_ProducesExpectedNumberOfSolutions_WithMaxDisplayedCount(int n, SolutionMode mode)
    {
        var formatter = new SolutionFormatter();
        var cap = NQueen.Domain.Settings.SimulationSettings.MaxDisplayedCount;
        var solver = new BitmaskSolver(n, mode, DisplayMode.Hide, formatter, maxSolutionsInOutput: cap)
        {
            UseCountOnlyUniqueMode = false,
            UseCountOnlyAllMode = false,
            EnableEvents = false
        };
        var ctx = new SimulationContext(n, mode, DisplayMode.Hide);
        var results = await Task.Run(() => solver.Solve());
        ulong expected = mode switch
        {
            SolutionMode.Unique => ExpectedSolutions.GetUniqueCount(n),
            SolutionMode.All => ExpectedSolutions.GetAllCount(n),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        int expectedMaterialized = (int)Math.Min((ulong)cap, expected);
        results.Solutions.Should().NotBeNull();
        results.Solutions.Count.Should().Be(expectedMaterialized, $"Materialize mode should produce min(MaxDisplayedCount, expected) solutions for {mode} N={n}.");
        results.SolutionsCount.Should().Be(expected, $"Total solutions count for {mode} N={n} should match expected.");
        foreach (var s in results.Solutions)
            s.BoardSize.Should().Be(n);
    }

    private const int _maxDsipayedCount = SimulationSettings.MaxDisplayedCount;
}
