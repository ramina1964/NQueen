using System.Linq;
namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Unique solution search: large boards use symmetry-pruned parallel counter; small boards use full canonical minimal enumeration.
    private void RunUniqueUnified(bool parallel)
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;
        var packedSample = new List<(UInt128 Packed, int BoardSize)>();
        int materialized = 0;
        int capReachedFlag = 0;

        // Configure global pruning for unique mode (incremental canonicalization disabled here)
        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        if (N >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            // Large boards: symmetry-pruned enumeration (parallel in engine) counting canonical minimal representatives.
            _solutionCount = Engines.SymmetryPrunedUniqueCounter.Count(N, cap, rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
                    // For large boards we skip packing to keep memory minimal.
                    packedSample.Add((0, N));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag, 1);
                    }
                }
            });
        }
        else
        {
            // Small boards: exhaustive enumeration with canonical minimality test; materialize up to cap packed canonical representatives.
            ulong uniqueCount = Engines.CanonicalUniqueSearchEngine.CountUnique(N, rows =>
            {
                if (System.Threading.Volatile.Read(ref capReachedFlag) == 1) return;
                if (materialized < Math.Max(1, cap))
                {
                    var packed = N <= 25 ? SymmetryHelper.PackCanonical(rows, N) : 0;
                    packedSample.Add((packed, N));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag, 1);
                    }
                }
            });
            _solutionCount = uniqueCount;
        }

        _solutions.AddRange(packedSample);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);
}
