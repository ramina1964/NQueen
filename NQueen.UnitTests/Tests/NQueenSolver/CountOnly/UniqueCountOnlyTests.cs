namespace NQueen.UnitTests.Tests.NQueenSolver.CountOnly;

[Collection("SolverBackend")]
[Trait("Category","CountOnly")]
public class UniqueCountOnlyTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(UniqueCases))]
    public async Task UniqueMode_CountOnly_Parallel_MatchesExpected(int n, ulong expected)
    {
        var solver = (BitmaskSolver)_solver;
        solver.UseCountOnlyUniqueMode = true;
        solver.UseParallel = true; // parallel path
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(expected);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    public async Task UniqueMode_CountOnly_Sequential_Equals_Parallel(int n)
    {
        var solver = (BitmaskSolver)_solver;
        // Parallel first
        solver.UseCountOnlyUniqueMode = true;
        solver.UseParallel = true;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var parallelRes = await _solver.GetSimResultsAsync(ctx);

        // Sequential
        solver.UseParallel = false;
        var seqRes = await _solver.GetSimResultsAsync(ctx);

        parallelRes.SolutionsCount.Should().Be(seqRes.SolutionsCount);
        seqRes.Solutions.Should().BeEmpty();
    }

    public static TheoryData<int, ulong> UniqueCases
    {
        get
        {
            var td = new TheoryData<int, ulong>();
            foreach (var kv in ExpectedSolutionCounts.UniqueCounts)
                td.Add(kv.Key, kv.Value);
            return td;
        }
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
