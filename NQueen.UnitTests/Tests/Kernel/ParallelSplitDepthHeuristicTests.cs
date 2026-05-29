namespace NQueen.UnitTests.Tests.Kernel;

public class ParallelSplitDepthHeuristicTests
{
    // ── GetOptimalSplitDepth ─────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  1)]
    [InlineData(8,  1)]
    [InlineData(10, 1)]
    public void GetOptimalSplitDepth_SmallBoard_Returns1(int n, int expected) =>
        ParallelSplitDepthHeuristic.GetOptimalSplitDepth(n).Should().Be(expected);

    [Theory]
    [InlineData(11, 2)]
    [InlineData(13, 2)]
    public void GetOptimalSplitDepth_MediumBoard_Returns2(int n, int expected) =>
        ParallelSplitDepthHeuristic.GetOptimalSplitDepth(n).Should().Be(expected);

    [Theory]
    [InlineData(14)]
    [InlineData(16)]
    [InlineData(20)]
    public void GetOptimalSplitDepth_LargeBoard_Returns2Or3(int n) =>
        ParallelSplitDepthHeuristic.GetOptimalSplitDepth(n).Should().BeInRange(2, 3);

    [Fact]
    public void GetOptimalSplitDepth_WithExplicitCoreCount_ReturnsPositive() =>
        ParallelSplitDepthHeuristic.GetOptimalSplitDepth(16, logicalCoreCount: 4).Should().BePositive();

    [Fact]
    public void GetOptimalSplitDepth_ZeroCoreCount_FallsBackToEnvironment() =>
        ParallelSplitDepthHeuristic.GetOptimalSplitDepth(16, logicalCoreCount: 0).Should().BePositive();

    // ── ShouldUseParallelForAll ──────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(11)]
    public void ShouldUseParallelForAll_SmallBoard_ReturnsFalse(int n) =>
        ParallelSplitDepthHeuristic.ShouldUseParallelForAll(n).Should().BeFalse();

    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    public void ShouldUseParallelForAll_LargeBoard_MultipleCores_ReturnsTrue(int n) =>
        ParallelSplitDepthHeuristic.ShouldUseParallelForAll(n, logicalCoreCount: 4).Should().BeTrue();

    [Fact]
    public void ShouldUseParallelForAll_LargeBoard_SingleCore_ReturnsFalse() =>
        ParallelSplitDepthHeuristic.ShouldUseParallelForAll(16, logicalCoreCount: 1).Should().BeFalse();

    // ── GetSplitPlan ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(8)]
    [InlineData(14)]
    [InlineData(20)]
    public void GetSplitPlan_ReturnsPlan_WithPositiveSplitDepthAndTargetRoots(int n)
    {
        var plan = ParallelSplitDepthHeuristic.GetSplitPlan(n);
        plan.SplitDepth.Should().BePositive();
        plan.TargetRoots.Should().BePositive();
    }

    [Fact]
    public void GetSplitPlan_LargeBoard_UseParallelIsTrue() =>
        ParallelSplitDepthHeuristic.GetSplitPlan(14, logicalCoreCount: 4).UseParallel.Should().BeTrue();

    [Fact]
    public void GetSplitPlan_SmallBoard_UseParallelIsFalse() =>
        ParallelSplitDepthHeuristic.GetSplitPlan(8).UseParallel.Should().BeFalse();

    [Fact]
    public void GetSplitPlan_ConsistentWithGetOptimalSplitDepth()
    {
        int n = 16;
        var plan = ParallelSplitDepthHeuristic.GetSplitPlan(n);
        plan.SplitDepth.Should().Be(ParallelSplitDepthHeuristic.GetOptimalSplitDepth(n));
    }
}
