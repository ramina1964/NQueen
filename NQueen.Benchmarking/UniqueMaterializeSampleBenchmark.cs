using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Enums;
using System;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking
{
    [CPUUsageDiagnoser]
    public class UniqueMaterializeSampleBenchmark
    {
        [Params(16)]
        public int BoardSize;
        private ISolutionFormatter _formatter = new DefaultSolutionFormatter();
        [Benchmark]
        public SimulationResults MaterializeSampleUnique()
        {
            var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
            {
                EnableEvents = false,
                UseCountOnlyUniqueMode = false
            };
            solver.ParallelRootSplitDepth = -1;
            var results = solver.Solve();
            if (results.Solutions.Count == 0)
                throw new InvalidOperationException();
            return results;
        }
    }
}