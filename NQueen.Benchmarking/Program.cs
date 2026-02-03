namespace NQueen.Benchmarking;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[CPUUsageDiagnoser]
public class NQueenBench
{
    [Params(20)]
    public int N { get; set; }

    [Benchmark]
    public long CountOnly()
    {
        return BitboardNQueenSolver.CountSolutions(N, parallel: true);
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Running NQueen benchmarks (Release)…");
        var benchMode = Environment.GetEnvironmentVariable("BENCHMARK_MODE");
        if ((benchMode == "1") || (args != null && args.Any(a => a.StartsWith("--"))))
        {
            // Run all benchmarks in this assembly when invoked by BenchmarkDotNet infrastructure.
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            Console.WriteLine("Done. See BenchmarkDotNet.Artifacts for detailed reports.");
            return;
        }

        // Default local run: execute the merged Unique + CountOnly high-N benchmark (N=18,19,20).
        BenchmarkRunner.Run<UniqueCountOnlyHighNBenchmark>();
    }
}
