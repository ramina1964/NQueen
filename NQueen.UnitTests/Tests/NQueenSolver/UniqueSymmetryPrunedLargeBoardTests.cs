using FluentAssertions;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Models;
using Xunit;

namespace NQueen.UnitTests.Tests.NQueenSolver;

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
        resultsMat.SolutionsCount.Should().BeGreaterThan(0);
        resultsMat.Solutions.Count.Should().BeLessThanOrEqualTo(cap);

        // CountOnly mode (should not materialize any solutions)
        var solverCnt = new BitmaskSolver(n, SolutionMode.Unique, DisplayMode.Hide, formatter, maxSolutionsInOutput: 0)
        {
            UseCountOnlyUniqueMode = true
        };
        var resultsCnt = solverCnt.Solve();
        resultsCnt.SolutionsCount.Should().Be(resultsMat.SolutionsCount);
        resultsCnt.Solutions.Should().BeEmpty();
    }
}
