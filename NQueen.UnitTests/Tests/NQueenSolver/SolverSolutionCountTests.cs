namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Counts")]
public class SolverSolutionCountTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task GetSimResults_UniqueMode_SmallBoards_CountMatchesExpected(int n)
    {
        ulong expected = ExpectedSolutions.GetUniqueCount(n);
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.SolutionsCount.Should().Be(expected, $"Unique solutions count for N={n} should match expected source.");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task GetSimResults_AllMode_SmallBoards_CountMatchesExpected(int n)
    {
        ulong expected = ExpectedSolutions.GetAllCount(n);
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.SolutionsCount.Should().Be(expected, $"All solutions count for N={n} should match expected source.");
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public async Task GetSimResults_UniqueMode_LargerBoards_CountMatchesExpected(int n)
    {
        _solver.UseCountOnlyUniqueMode = true;
        _solver.UseCountOnlyAllMode = false;
        ulong expected = ExpectedSolutions.GetUniqueCount(n);
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.Should().BeEmpty();
        results.SolutionsCount.Should().Be(expected);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public async Task GetSimResults_AllMode_LargerBoards_CountMatchesExpected(int n)
    {
        _solver.UseCountOnlyAllMode = true;
        _solver.UseCountOnlyUniqueMode = false;
        ulong expected = ExpectedSolutions.GetAllCount(n);
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.Should().BeEmpty();
        results.SolutionsCount.Should().Be(expected);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task GetSimResults_SingleMode_SmallBoards_CountIsOne(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.SolutionsCount.Should().Be(1UL, $"Single mode should return exactly one solution for N={n}");
        results.Solutions.Should().ContainSingle();
    }
}
