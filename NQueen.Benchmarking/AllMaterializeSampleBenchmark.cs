using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Enums;

namespace NQueen.Benchmarking
{
 [MemoryDiagnoser]
 public class AllMaterializeSampleBenchmark
 {
 [Params(12)]
 public int BoardSize { get; set; }

 [Params(5)]
 public int SampleCap { get; set; }

 private ISolutionFormatter _formatter = new SolutionFormatter();

 [Benchmark(Baseline = true)]
 public SimulationResults MaterializeSampleAll()
 {
 var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput: SampleCap)
 {
 UseCountOnlyAllMode = false,
 EnableEvents = false
 };
 var results = solver.Solve();
 if (results.Solutions.Count ==0)
 throw new InvalidOperationException();
 return results;
 }

 [Benchmark]
 public SimulationResults CountOnlyAll()
 {
 var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter, maxSolutionsInOutput:0)
 {
 UseCountOnlyAllMode = true,
 EnableEvents = false
 };
 var results = solver.Solve();
 return results;
 }
 }
}
