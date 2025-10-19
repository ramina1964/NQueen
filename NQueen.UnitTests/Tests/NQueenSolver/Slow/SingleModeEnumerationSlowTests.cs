namespace NQueen.UnitTests.Tests.NQueenSolver.Slow;

[Collection("SolverBackend")]
[Trait("Category","Slow")]
public class SingleModeEnumerationSlowTests(SolverBackEndFixture fixture)
{
    // Larger single-mode boards separated to allow filtering during fast dev cycles.
    [Theory]
    [MemberData(nameof(SlowSingleBoards))]
    public async Task SingleMode_LargerBoards_StillOneSolution(int boardSize)
    {
        var ctx = new SimulationContext(boardSize, SolutionMode.Single, DisplayMode.Hide);
        var expected = TestBase.FetchExpectedSols(ctx);
        expected.Should().ContainSingle();
        var res = await _solver.GetSimResultsAsync(ctx);
        res.Solutions.Should().ContainSingle();
        res.Solutions[0].QueenPositions.ToArray().Should().BeEquivalentTo(expected[0]);
    }

    public static TheoryData<int> SlowSingleBoards => new()
    {
        {9}, {10}, {11}, {12}, {13}
    };

    private readonly ISolverBackEnd _solver = fixture.Sut;
}
