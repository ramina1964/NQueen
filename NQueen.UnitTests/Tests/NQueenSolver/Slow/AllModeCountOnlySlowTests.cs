namespace NQueen.UnitTests.Tests.NQueenSolver.Slow;

[Collection("SolverBackend")]
[Trait("Category","Slow")]
public class AllModeCountOnlySlowTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(LargeAllCounts))]
    public async Task AllMode_CountOnly_LargeBoards(int n, ulong expectedAll)
    {
        var solver = (BitmaskSolver)_solver;
        solver.UseCountOnlyAllMode = true;
        solver.UseParallel = true;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(expectedAll);
    }

    public static TheoryData<int, ulong> LargeAllCounts => new()
    {
        {9, 352}, {10, 724}, {11, 2680}, {12, 14200}, {13, 73712}, {14, 365596}
    };

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
