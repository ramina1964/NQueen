namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "HalfBoardFlag")]
[Trait("Speed", "Slow")]
public class HalfBoardFlagAllModeTests(SolverBackEndFixture fixture)
{
    // Test sizes (ensure enumeration path, below lookup threshold 20)
    public static TheoryData<int> Sizes => [15, 16, 17];

    [Theory]
    [MemberData(nameof(Sizes))]
    public async Task CountOnly_AllMode_FlagOn_MatchesExpected(int n)
    {
        bool origCountOnly = _solver.UseCountOnlyAllMode;
        bool origHalf = _solver is BitmaskSolver bsOrig && bsOrig.EnableHalfBoardRestriction;
        try
        {
            _solver.UseCountOnlyAllMode = true; // count-only path
            if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = true;
            var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
            var res = await _solver.GetSimResultsAsync(ctx);
            res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
            res.Solutions.Should().BeEmpty();
        }
        finally
        {
            _solver.UseCountOnlyAllMode = origCountOnly;
            if (_solver is BitmaskSolver bsRestore) bsRestore.EnableHalfBoardRestriction = origHalf;
        }
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public async Task CountOnly_AllMode_FlagOff_MatchesExpected(int n)
    {
        bool origCountOnly = _solver.UseCountOnlyAllMode;
        bool origHalf = _solver is BitmaskSolver bsOrig && bsOrig.EnableHalfBoardRestriction;
        try
        {
            _solver.UseCountOnlyAllMode = true;
            if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = false;
            var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
            var res = await _solver.GetSimResultsAsync(ctx);
            res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
            res.Solutions.Should().BeEmpty();
        }
        finally
        {
            _solver.UseCountOnlyAllMode = origCountOnly;
            if (_solver is BitmaskSolver bsRestore) bsRestore.EnableHalfBoardRestriction = origHalf;
        }
    }

    [Fact]
    public async Task Materialize_AllMode_FlagOn_DoesNotAlterCount()
    {
        int n = 15;
        bool origCountOnly = _solver.UseCountOnlyAllMode;
        bool origHalf = _solver is BitmaskSolver bsOrig && bsOrig.EnableHalfBoardRestriction;
        try
        {
            _solver.UseCountOnlyAllMode = false;
            if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = true;
            var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
            var res = await _solver.GetSimResultsAsync(ctx);
            res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
            res.Solutions.Count.Should().BeGreaterThan(0);
            (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
        }
        finally
        {
            _solver.UseCountOnlyAllMode = origCountOnly;
            if (_solver is BitmaskSolver bsRestore) bsRestore.EnableHalfBoardRestriction = origHalf;
        }
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
