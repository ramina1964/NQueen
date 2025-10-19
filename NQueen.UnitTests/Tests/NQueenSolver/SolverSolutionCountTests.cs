namespace NQueen.UnitTests.Tests.NQueenSolver;

/// <summary>
/// Verifies that total solution counts returned by the solver match the known expected counts
/// for a range of board sizes and modes. Uses SolutionCounts (Domain unified source).
/// Focused on count correctness; solution materialization is covered elsewhere.
/// </summary>
[Collection("SolverBackend")]
[Trait("Category", "Counts")]
public class SolverSolutionCountTests(SolverBackEndFixture fixture)
{
    private readonly ISolverBackEnd _solver = fixture.Sut;

    // Small boards where we can enumerate fully and compare exact counts for Unique and All.
    // Use Hide mode to avoid visualization overhead.
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task UniqueMode_CountMatchesExpected(int n)
    {
        // Arrange
        ulong expected = SolutionCounts.GetUnique(n);
        expected.Should().BeGreaterThanOrEqualTo(0UL);
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.Should().Be(expected, $"Unique solutions count for N={n} should match expected source.");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task AllMode_CountMatchesExpected(int n)
    {
        // Arrange
        ulong expected = SolutionCounts.GetAll(n);
        expected.Should().BeGreaterThanOrEqualTo(0UL);
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.Should().Be(expected, $"All solutions count for N={n} should match expected source.");
    }

    // Larger boards: ensure solver returns a positive count equal to expected table (sanity, performance path).
    // For All mode we explicitly force count-only to avoid cap truncation.
    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public async Task UniqueMode_LargerBoards_CountMatchesExpected(int n)
    {
        // Force count-only to ensure full unique enumeration beyond materialization cap.
        _solver.UseCountOnlyUniqueMode = true;
        _solver.UseCountOnlyAllMode = false;
        ulong expected = SolutionCounts.GetUnique(n);
        expected.Should().BeGreaterThan(0UL, "Expected unique count must be > 0 for larger boards.");
        var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
        var results = await _solver.GetSimResultsAsync(ctx);
        results.Solutions.Should().BeEmpty("Count-only unique mode should not materialize solutions for large boards.");
        results.SolutionsCount.Should().Be(expected);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public async Task AllMode_LargerBoards_CountMatchesExpected(int n)
    {
        // Force count-only to bypass materialization cap and ensure full enumeration.
        _solver.UseCountOnlyAllMode = true;
        _solver.UseCountOnlyUniqueMode = false;
        ulong expected = SolutionCounts.GetAll(n);
        expected.Should().BeGreaterThan(0UL, "Expected all count must be > 0 for larger boards.");
        var ctx = new SimulationContext(n, SolutionMode.All, DisplayMode.Hide);

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.Solutions.Should().BeEmpty("Count-only mode should not materialize solutions for large All boards.");
        results.SolutionsCount.Should().Be(expected);
    }

    // Single mode: Only verifies that exactly one solution exists for sizes where a single solution is defined (subset check).
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task SingleMode_CountIsOne(int n)
    {
        // Arrange
        var ctx = new SimulationContext(n, SolutionMode.Single, DisplayMode.Hide);

        // Act
        var results = await _solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.Should().Be(1UL, $"Single mode should return exactly one solution for N={n}");
        results.Solutions.Should().ContainSingle();
    }
}
