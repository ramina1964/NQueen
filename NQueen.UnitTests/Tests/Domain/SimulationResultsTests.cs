namespace NQueen.UnitTests.Tests.Domain;

public class SimulationResultsTests
{
    private static readonly ISolutionFormatter Formatter = new SolutionFormatter();

    private static Solution MakeSolution(int id) => new([0, 1], Formatter, id: id);

    [Fact]
    public void PreferredConstructor_KeepsExplicitTotal_NotInferred()
    {
        var solutions = new List<Solution> { MakeSolution(1), MakeSolution(2) };

        var results = new SimulationResults(solutions, totalSolutions: 100UL, ElapsedTimeInSec: 1.5);

        results.SolutionsCount.ShouldBe(100UL);
        results.IsTotalInferred.ShouldBeFalse();
        results.ElapsedTimeInSec.ShouldBe(1.5);
        results.Solutions.Count.ShouldBe(2);
    }

    [Fact]
    public void LegacyConstructor_InfersTotalFromList()
    {
        var solutions = new List<Solution> { MakeSolution(1), MakeSolution(2), MakeSolution(3) };

        var results = new SimulationResults(solutions, ElapsedTimeInSec: 0.25);

        results.SolutionsCount.ShouldBe(3UL);
        results.IsTotalInferred.ShouldBeTrue();
    }

    [Fact]
    public void IsTruncated_True_WhenTotalExceedsMaterializedList()
    {
        var solutions = new List<Solution> { MakeSolution(1) };

        var results = new SimulationResults(solutions, totalSolutions: 5UL, ElapsedTimeInSec: 0.1);

        results.IsTruncated.ShouldBeTrue();
    }

    [Fact]
    public void IsTruncated_False_WhenTotalMatchesListCount()
    {
        var solutions = new List<Solution> { MakeSolution(1), MakeSolution(2) };

        var results = new SimulationResults(solutions, totalSolutions: 2UL, ElapsedTimeInSec: 0.1);

        results.IsTruncated.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_EmptySolutions_ProducesEmptyListAndZeroInferredCount()
    {
        var results = new SimulationResults([], ElapsedTimeInSec: 0.0);

        results.Solutions.ShouldBeEmpty();
        results.SolutionsCount.ShouldBe(0UL);
        results.IsTotalInferred.ShouldBeTrue();
    }
}
