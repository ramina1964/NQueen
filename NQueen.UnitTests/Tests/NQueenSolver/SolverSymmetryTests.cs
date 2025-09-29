namespace NQueen.UnitTests.Tests.NQueenSolver;

public class SolverSymmetryTests
{
    [Fact]
    public void DetectsSymmetricalSolutions()
    {
        // Arrange
        var comparer = new MemoryIntArrayComparer();
        var solutions = new HashSet<Memory<int>>(comparer);

        // Add a base solution
        var baseSolution = new Memory<int>([0, 2, 4, 1, 3]);
        solutions.Add(baseSolution);

        // Generate symmetrical transformations
        var symmetricalSolutions = SymmetryHelper.GetSymmetricalTransformations(baseSolution.Span.ToArray());

        // Add symmetrical transformations to the solutions set
        foreach (var symmetrical in symmetricalSolutions)
            solutions.Add(new Memory<int>(symmetrical));

        // Act & Assert
        foreach (var symmetrical in symmetricalSolutions)
        {
            var memorySolution = new Memory<int>(symmetrical);
            bool exists = ContainsSolution(memorySolution);

            // Log the result of the assertion
            Debug.WriteLine($"Checking symmetrical solution: {string.Join(",", symmetrical)} - Exists: {exists}");

            exists.Should().BeTrue($"Symmetrical solution {string.Join(",", symmetrical)} should be detected.");
        }

        bool ContainsSolution(Memory<int> solution) =>
            solutions.Contains(solution);
    }
}
