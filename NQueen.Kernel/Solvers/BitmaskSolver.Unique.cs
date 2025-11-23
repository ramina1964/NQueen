namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Consolidated Unique mode executor: handles both large-board symmetry-pruned and small-board canonical enumeration.
    private void ExecuteUniqueModeUnified()
    {
        int boardSize = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount = 0;
        List<(UInt128 packed, int boardSize)> packedSample = [];
        int materialized = 0;
        int capReachedFlag = 0;

        Engines.SearchOptimizations.Configure(
            prefixMinimality: EnablePrefixMinimalityPruning,
            reflectionPruning: EnablePartialReflectionPruning,
            incrementalCanonicalization: EnableIncrementalCanonicalization);

        if (boardSize >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            _solutionCount = Engines.SymmetryPrunedUniqueCounter.Count(boardSize, cap, rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
                    packedSample.Add((0, boardSize));
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
            ulong uniqueCount = Engines.CanonicalUniqueSearchEngine.CountUnique(boardSize, rows =>
            {
                if (System.Threading.Volatile.Read(ref capReachedFlag) == 1) return;
                if (materialized < Math.Max(1, cap))
                {
                    var packed = boardSize <= 25 ? SymmetryHelper.PackCanonical(rows, boardSize) : 0;
                    packedSample.Add((packed, boardSize));
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
