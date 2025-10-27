using BenchmarkDotNet.Attributes;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Models;
using NQueen.Domain.Enums;
using System;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking
{
    [CPUUsageDiagnoser]
    public class UniqueSolutionCounterPackedBenchmark
    {
        [Params(16)]
        public int BoardSize;
        private ISolutionFormatter _formatter = new DefaultSolutionFormatter();
        [Benchmark]
        public ulong CountUniquePacked()
        {
            var solver = new BitmaskSolver(BoardSize, SolutionMode.Unique, DisplayMode.Hide, _formatter)
            {
                EnableEvents = false,
                UseCountOnlyUniqueMode = true
            };
            var results = solver.Solve();
            // Prevent JIT elision
            if (results.SolutionsCount == 0)
                throw new InvalidOperationException();
            return results.SolutionsCount;
        }
    }
}