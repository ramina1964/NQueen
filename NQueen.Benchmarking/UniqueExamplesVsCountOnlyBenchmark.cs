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
        var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter, SimulationSettings.MaxDisplayedCount)
        {
            UseCountOnlyUniqueMode = false,
            EnableEvents = false
        };
        return solver.Solve();
    }

    [Benchmark]
    public SimulationResults CountOnly()
    {
        var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter, 0)
        {
            UseCountOnlyUniqueMode = true,
            EnableEvents = false
        };
        return solver.Solve();
    }
}
