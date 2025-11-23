namespace NQueen.Benchmarking;

// Focused benchmark: N=16, Unique, Visualize, GUI-like event handlers only.
// Helps quantify current per-event & solution materialization overhead before optimization.
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.HostProcess, launchCount: 1, warmupCount: 0, iterationCount: 1)]
public class SolverEventOverheadGui16Benchmark
{
    private const int BoardSize = 16;
    private const SolutionMode Mode = SolutionMode.Unique;
    private const DisplayMode Display = DisplayMode.Visualize;

    private ISolutionFormatter _formatter = new DefaultSolutionFormatter();

    [Benchmark]
    public SimulationResults Solve()
    {
        var solver = new BitmaskSolver(BoardSize, Mode, Display, _formatter)
        {
            DelayInMillisec = 0
        };

        var cap = SimulationSettings.MaxDisplayedCount;
        var solutionsOc = new ObservableCollection<Solution>();
        int queenPlacedCounter = 0;
        int progressCounter = 0;

        // QueenPlaced handler approximating MainViewModel.OnQueenPlaced depth walk
        solver.QueenPlaced += (_, e) =>
        {
            var span = e.Solution.Span;
            int max = Math.Min(BoardSize, span.Length);
            int depth = 0;
            for (int col = 0; col < max; col++)
            {
                int row = span[col];
                if (row < 0) break;
                bool conflict = false;
                for (int prev = 0; prev < col; prev++)
                {
                    int prow = span[prev];
                    if (prow == row || Math.Abs(prow - row) == col - prev)
                    { conflict = true; break; }
                }
                if (conflict) break;
                depth = col + 1;
            }
            queenPlacedCounter += depth;
        };

        solver.SolutionFound += (_, e) =>
        {
            int nextId = solutionsOc.Count + 1;
            if (cap > 0 && solutionsOc.Count >= cap) return;
            var sol = new Solution(e.Solution.ToArray(), _formatter, nextId);
            solutionsOc.Add(sol);
        };

        solver.ProgressValueChanged += (_, _) => progressCounter++;

        var results = solver.Solve();
        if (queenPlacedCounter < -1 || progressCounter < -1) throw new InvalidOperationException();
        return results;
    }
}
