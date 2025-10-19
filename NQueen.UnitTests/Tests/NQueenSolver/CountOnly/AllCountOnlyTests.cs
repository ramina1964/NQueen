namespace NQueen.UnitTests.Tests.NQueenSolver.CountOnly;

[Collection("SolverBackend")]
[Trait("Category","CountOnly")]
public class AllCountOnlyTests(SolverBackEndFixture fixture)
{
    [Theory]
    [MemberData(nameof(AllCases))]
    public async Task AllMode_CountOnly_MatchesExpectedAndNoMaterialization(int n, ulong expected)
    {
        var solver = (BitmaskSolver)_solver; // ensure access to flags
        solver.UseCountOnlyAllMode = true;
        solver.UseParallel = true; // fast path
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().BeEmpty();
        res.SolutionsCount.Should().Be(expected);
    }

    public static TheoryData<int, ulong> AllCases
    {
        get
        {
            var td = new TheoryData<int, ulong>();
            foreach (var kv in ExpectedSolutionCounts.AllCounts)
                td.Add(kv.Key, kv.Value);
            return td;
        }
    }

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
