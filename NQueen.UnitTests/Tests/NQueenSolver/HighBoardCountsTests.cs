using NQueen.UnitTests.Fixtures;
using NQueen.Domain.Context;
using NQueen.Domain.Enums;
using NQueen.Domain.Utils;
using NQueen.Domain.Models;
using FluentAssertions;

namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
public class HighBoardCountsTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    // Board sizes with authoritative counts available (20..29)
    public static TheoryData<int> HighBoards => new() { 20,21,22,23,24,25,26,27,28,29 };

    // Smaller subset for materialization (enumerating one sample solution for very large boards can be expensive)
    public static TheoryData<int> MaterializeSampleBoards => new() { 20,21 }; // keep light

    [Theory]
    [MemberData(nameof(HighBoards))]
    [Trait("Category","HighBoard")]
    [Trait("Category","Fast")]
    public async Task AllMode_CountOnly_LookupMatches(int n)
    {
        // Arrange
        _solver.UseCountOnlyAllMode = true;
        _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        ulong expected = ExpectedSolutionCounts.GetAll(n);
        expected.Should().BeGreaterThan(0UL, "Lookup must have authoritative all count");

        // Act
        var res = await _solver.GetSimResultsAsync(ctx);

        // Assert
        res.SolutionsCount.Should().Be(expected);
        res.Solutions.Should().BeEmpty("Count-only mode must not materialize solutions");
    }

    [Theory]
    [MemberData(nameof(HighBoards))]
    [Trait("Category","HighBoard")]
    [Trait("Category","Fast")]
    public async Task UniqueMode_CountOnly_LookupMatches(int n)
    {
        _solver.UseCountOnlyUniqueMode = true;
        _solver.UseCountOnlyAllMode = false;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        ulong expected = ExpectedSolutionCounts.GetUnique(n);
        expected.Should().BeGreaterThan(0UL, "Lookup must have authoritative unique count");

        var res = await _solver.GetSimResultsAsync(ctx);

        res.SolutionsCount.Should().Be(expected);
        res.Solutions.Should().BeEmpty("Count-only mode must not materialize unique solutions");
    }

    [Theory]
    [MemberData(nameof(MaterializeSampleBoards))]
    [Trait("Category","HighBoard")]
    [Trait("Category","Slow")]
    public async Task AllMode_Materialize_SampleSolutionsCapRespected(int n)
    {
        _solver.UseCountOnlyAllMode = false;
        _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);
        ulong expected = ExpectedSolutionCounts.GetAll(n);
        expected.Should().BeGreaterThan(0UL);

        var res = await _solver.GetSimResultsAsync(ctx);

        res.SolutionsCount.Should().Be(expected);
        res.Solutions.Count.Should().BeGreaterThan(0);
        (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
        foreach (var sol in res.Solutions)
        {
            sol.BoardSize.Should().Be(n);
            sol.QueenPositions.Length.Should().Be(n);
        }
    }

    [Theory]
    [MemberData(nameof(MaterializeSampleBoards))]
    [Trait("Category","HighBoard")]
    [Trait("Category","Slow")]
    public async Task UniqueMode_Materialize_SampleSolutionsCapRespected(int n)
    {
        _solver.UseCountOnlyAllMode = false;
        _solver.UseCountOnlyUniqueMode = false;
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        ulong expected = ExpectedSolutionCounts.GetUnique(n);
        expected.Should().BeGreaterThan(0UL);

        var res = await _solver.GetSimResultsAsync(ctx);

        res.SolutionsCount.Should().Be(expected);
        res.Solutions.Count.Should().BeGreaterThan(0);
        (res.Solutions.Count <= SimulationSettings.MaxDisplayedCount).Should().BeTrue();
        foreach (var sol in res.Solutions)
        {
            sol.BoardSize.Should().Be(n);
            sol.QueenPositions.Length.Should().Be(n);
        }
    }

    [Theory]
    [MemberData(nameof(HighBoards))]
    [Trait("Category","HighBoard")]
    [Trait("Category","Fast")]
    public async Task SingleMode_LargeBoards_ReturnsExactlyOneSolution(int n)
    {
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);
        var res = await _solver.GetSimResultsAsync(ctx);
        res.SolutionsCount.Should().Be(1UL);
        res.Solutions.Should().ContainSingle();
        res.Solutions[0].QueenPositions.Length.Should().Be(n);
    }
}
