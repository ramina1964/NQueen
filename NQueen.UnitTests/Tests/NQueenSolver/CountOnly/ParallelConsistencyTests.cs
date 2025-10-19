namespace NQueen.UnitTests.Tests.NQueenSolver.CountOnly;

[Collection("SolverBackend")]
[Trait("Category","CountOnly")]
public class ParallelConsistencyTests(SolverBackEndFixture fixture)
{
    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    public async Task AllMode_ParallelVsSequential_CountsEqual(int n)
    {
        var solver = (BitmaskSolver)_solver;
        solver.UseCountOnlyAllMode = true;
        solver.UseParallel = true;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var parallelRes = await _solver.GetSimResultsAsync(ctx);

        solver.UseParallel = false;
        var sequentialRes = await _solver.GetSimResultsAsync(ctx);

        parallelRes.SolutionsCount.Should().Be(sequentialRes.SolutionsCount);
        sequentialRes.Solutions.Should().BeEmpty();
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
