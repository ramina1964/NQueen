using System.Linq;
namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Shared unique solution search core (now always uses symmetry-pruned for large boards)
    private void RunUniqueUnified(bool parallel)
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear();
        _eventsSuppressedAfterCap = false;
        _solutionCount =0;
        var packedSample = new List<(UInt128 Packed, int BoardSize)>();
        int materialized =0;
        int capReachedFlag =0;

        // Use symmetry-pruned for large boards, canonical for small
        if (N >= SimulationSettings.LargeBoardSymmetryPruningThreshold)
        {
            _solutionCount = UniqueSolutionCounter.Count(N, null, _currentSimToken, ProgressValueChanged, this, false, cap, rows =>
            {
                if (materialized < Math.Max(1, cap))
                {
                    packedSample.Add((0, N)); // No canonical packing for large boards
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag,1);
                    }
                }
            });
        }
        else
        {
            ulong uniqueCount = CanonicalUniqueSearchEngine.CountUnique(N, rows =>
            {
                if (System.Threading.Volatile.Read(ref capReachedFlag) ==1) return;
                if (materialized < Math.Max(1, cap))
                {
                    var packed = N <=25 ? SymmetryHelper.PackCanonical(rows, N) :0;
                    packedSample.Add((packed, N));
                    materialized++;
                    if (materialized >= cap && _capEnabled)
                    {
                        _eventsSuppressedAfterCap = true;
                        System.Threading.Volatile.Write(ref capReachedFlag,1);
                    }
                }
            });
            _solutionCount = uniqueCount;
        }
        // Only keep materialized solutions
        _solutions.AddRange(packedSample);
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);
}
