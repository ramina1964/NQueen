namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Consolidated Unique mode executor: handles both large-board symmetry-pruned and small-board canonical enumeration.
    private void ExecuteUniqueModeUnified()
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;
        var packedSample = new List<(UInt128 Packed, int BoardSize)>();
        int materialized = 0;
        int capReachedFlag = 0;

        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        if (N >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            _solutionCount = Engines.SymmetryPrunedUniqueCounter.Count(N, cap, rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
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
}
