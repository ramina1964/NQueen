namespace NQueen.UnitTests.Tests.NQueenSolver.Slow;

[Collection("SolverBackend")]
[Trait("Category","Slow")]
public class UniqueModeCountOnlySlowTests(SolverBackEndFixture fixture)
{
    // Larger boards unique count-only verification (no materialization) kept separate.
    [Theory]
    [MemberData(nameof(LargeUniqueCounts))]
    public async Task UniqueMode_CountOnly_LargeBoards(int n, ulong expectedUnique)
    {
        var solver = (BitmaskSolver)_solver;
        solver.UseCountOnlyUniqueMode = true;
        solver.UseParallel = true;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(expectedUnique);
    }

    public static TheoryData<int, ulong> LargeUniqueCounts => new()
    {
        // Fundamental counts beyond small enumeration set
        {9, 46}, {10, 92}, {11, 341}, {12, 1787}, {13, 9233}, {14, 45752}
    };

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
