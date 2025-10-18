namespace NQueen.Benchmarking;

[MemoryDiagnoser]
public class UniqueExamplesVsCountOnlyBenchmark
{
    [Params(12, 13, 14, 15, 16)]
    public int BoardSize;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark]
    public SimulationResults MaterializeExamples()
    {
        var context = new SimulationContext(BoardSize, SolutionMode.Unique, DisplayMode.Hide);
        var solver = new UniqueExamplesOrCountSolver(_formatter, exampleCap: SimulationSettings.MaxNoOfSolutionsInOutput);
        var (examples, _) = solver.Solve(context);
        return examples;
    }

    [Benchmark]
    public SimulationResults CountOnly()
    {
        var context = new SimulationContext(BoardSize, SolutionMode.Unique, DisplayMode.Hide);
        var solver = new UniqueExamplesOrCountSolver(_formatter, exampleCap: 0);
        var (_, countOnly) = solver.Solve(context, countOnly: true);
        return countOnly;
    }
}
