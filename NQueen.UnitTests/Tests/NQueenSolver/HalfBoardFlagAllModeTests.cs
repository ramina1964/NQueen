namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "HalfBoardFlag")]
public class HalfBoardFlagAllModeTests(SolverBackEndFixture fixture)
{
    // Test sizes (ensure enumeration path, below lookup threshold 20)
    public static TheoryData<int> Sizes => [15, 16, 17];

    [Theory]
    [MemberData(nameof(Sizes))]
    public async Task CountOnly_AllMode_FlagOn_MatchesExpected(int n)
    {
        _solver.UseCountOnlyAllMode = true; // count-only path
        if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = true;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        res.Solutions.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public async Task CountOnly_AllMode_FlagOff_MatchesExpected(int n)
    {
        _solver.UseCountOnlyAllMode = true;
        if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        res.Solutions.Should().BeEmpty();
    }

    [Fact]
    public async Task Materialize_AllMode_FlagOn_DoesNotAlterCount()
    {
        int n = 15;
        _solver.UseCountOnlyAllMode = false;
        if (_solver is BitmaskSolver bs) bs.EnableHalfBoardRestriction = true;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(ExpectedSolutionCounts.GetAll(n));
        res.Solutions.Count.Should().BeGreaterThan(0);
        (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
