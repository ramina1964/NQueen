using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NQueen.Kernel.Solvers;
using NQueen.Domain.Enums;
using NQueen.Domain.Interfaces;
using System.Collections.Generic;
using NQueen.Domain.Models;
using Microsoft.VSDiagnostics;

namespace NQueen.Benchmarking
{
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [CPUUsageDiagnoser]
    public class AllCountOnlyN18Benchmark
    {
        [Params(18)]
        public int BoardSize { get; set; }

        private readonly ISolutionFormatter _formatter = new NoopFormatter();
        [Benchmark(Baseline = true, Description = "All Parallel Count-Only (N=18)")]
        public ulong All_Parallel_CountOnly_N18()
        {
            using var solver = new BitmaskSolver(BoardSize, SolutionMode.All, DisplayMode.Hide, _formatter)
            {
                EnableEvents = false,
                UseParallel = true,
                UseCountOnlyAllMode = true,
                EnablePrefixMinimalityPruning = false,
                EnablePartialReflectionPruning = false,
                EnableHalfBoardRestriction = BoardSize >= 15,
                ParallelRootSplitDepth = 3,
                UseAdaptiveDepth = true
            };
            solver.SetSimulationToken(Guid.NewGuid());
            return solver.Solve().SolutionsCount;
        }

        private sealed class NoopFormatter : ISolutionFormatter
        {
            public string FormatSolutions(IReadOnlyList<Position> queenPositions, IndexingType indexingType, int boardSize) => string.Empty;
        }
    }
}