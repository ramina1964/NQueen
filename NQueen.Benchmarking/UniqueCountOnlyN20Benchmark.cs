using System;
using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Interfaces;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking
{
    [CPUUsageDiagnoser]
    public class UniqueCountOnlyN20Benchmark
    {
        [Params(20)]
        public int BoardSize { get; set; }

        private readonly ISolutionFormatter _formatter = new DefaultSolutionFormatter();
        [Benchmark(Baseline = true, Description = "Unique Count-Only (N=20)")]
        public ulong Unique_CountOnly_N20()
        {
            using var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
            {
                EnableEvents = false,
                UseParallel = true,
                UseCountOnlyUniqueMode = true,
                EnablePrefixMinimalityPruning = true,
                EnablePartialReflectionPruning = true,
                EnableHalfBoardRestriction = BoardSize >= 15,
                ParallelRootSplitDepth = 3,
                UseAdaptiveDepth = true
            };
            solver.SetSimulationToken(Guid.NewGuid());
            return solver.Solve().SolutionsCount;
        }
    }
}