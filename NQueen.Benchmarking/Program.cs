namespace NQueen.Benchmarking;

internal class Program
{
    static void Main(string[] args)
    {
        var artifactsPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NQueenBenchArtifacts");
        var manual = BenchmarkDotNet.Configs.ManualConfig.Create(BenchmarkDotNet.Configs.DefaultConfig.Instance);
        manual.ArtifactsPath = artifactsPath;
        BenchmarkDotNet.Running.BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, manual);
    }
}
