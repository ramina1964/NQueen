namespace NQueen.UnitTests.Tests.NQueenSolver;

[Collection("SolverBackend")]
[Trait("Category", "Counts")]
public class UniqueCountingAccuracyTests(SolverBackEndFixture fixture)
{
 private readonly ISolverBackEnd _solver = fixture.Sut;

 public static TheoryData<int> Boards => new()
 {
4,5,6,7,8,9,10,11
 };

 [Theory]
 [MemberData(nameof(Boards))]
 public async Task UniqueCounting_TotalMatchesExpected(int n)
 {
 bool origFlag = _solver.UseCountOnlyUniqueMode;
 try
 {
 _solver.UseCountOnlyUniqueMode = false; // ensure enumeration path
 var ctx = new SimulationContext(n, SolutionMode.Unique, DisplayMode.Hide);
 var results = await _solver.GetSimResultsAsync(ctx);
 ulong expected = ExpectedSolutionCounts.GetUnique(n);
 results.SolutionsCount.Should().Be(expected, $"Fundamental unique count should match curated data for N={n}.");
 }
 finally
 {
 _solver.UseCountOnlyUniqueMode = origFlag;
 }
 }
}
