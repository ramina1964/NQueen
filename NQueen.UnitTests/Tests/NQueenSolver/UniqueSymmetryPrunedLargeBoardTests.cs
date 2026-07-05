namespace NQueen.UnitTests.Tests.NQueenSolver;

[Trait("Category", "Slow")]
public class UniqueSymmetryPrunedLargeBoardTests
{
    [Theory]
    [InlineData(15, 5)] // N=15, cap=5
    public void UniqueMaterializeAndCountOnly_CorrectnessAndCap(int n, int cap)
    {
        var formatter = new SolutionFormatter();
        // Materialize mode (should materialize up to cap)
        var solverMat = new BitmaskSolver(n, SolutionMode.Unique, DisplayMode.Hide, formatter, maxSolutionsInOutput: cap)
        {
            UseCountOnlyUniqueMode = false
        };
        var resultsMat = solverMat.Solve();
        resultsMat.SolutionsCount.ShouldBeGreaterThan(0UL);
        resultsMat.Solutions.Count.ShouldBeLessThanOrEqualTo(cap);

        // CountOnly mode (should not materialize any solutions)
        var solverCnt = new BitmaskSolver(n, SolutionMode.Unique, DisplayMode.Hide, formatter, maxSolutionsInOutput: 0)
        {
            UseCountOnlyUniqueMode = true
        };
        var resultsCnt = solverCnt.Solve();
        resultsCnt.SolutionsCount.ShouldBe(resultsMat.SolutionsCount);
        resultsCnt.Solutions.ShouldBeEmpty();
    }
}
