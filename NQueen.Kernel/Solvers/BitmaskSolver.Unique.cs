using System.Linq;
namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private const int AggressiveSymmetryThreshold = 12;

    private static int EstimateUniqueCapacity(int n)
    {
        return n switch
        {
            8 => 32,
            10 => 128,
            12 => 256,
            14 => 1024,
            16 => 4096,
            _ => 1 << (n > 20 ? 20 : n)
        };
    }

    private struct PackedSolution
    {
        public UInt128 Packed;
        public int BoardSize;
        public PackedSolution(UInt128 packed, int boardSize)
        {
            Packed = packed;
            BoardSize = boardSize;
        }
    }

    // Shared unique solution search core (materialize then count) using new fundamental enumeration engine
    private void RunUniqueUnified(bool parallel)
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear(); _rawSolutions = null; _eventsSuppressedAfterCap = false; _solutionCount = 0;
        var rawSample = new List<int[]>();
        var packedSample = new List<PackedSolution>();
        int materialized = 0;
        int capReachedFlag = 0;

        // Use parallel fundamental enumerator regardless of 'parallel' flag for performance; could fallback if N small
        ulong uniqueCount = Engines.FundamentalUniqueEnumerationEngine.Enumerate(N, cap, rows =>
        {
            if (System.Threading.Volatile.Read(ref capReachedFlag) == 1) return;
            if (materialized < cap)
            {
                var storedCopy = new int[N];
                Array.Copy(rows, storedCopy, N);
                rawSample.Add(storedCopy);
                var packed = N <= 25 ? SymmetryHelper.PackCanonical(rows, N) : 0;
                packedSample.Add(new PackedSolution(packed, N));
                materialized++;
                if (materialized >= cap && _capEnabled)
                {
                    _eventsSuppressedAfterCap = true;
                    System.Threading.Volatile.Write(ref capReachedFlag, 1);
                }
            }
        });
        _solutionCount = uniqueCount;
        _rawSolutions = rawSample;
        _solutions.AddRange(packedSample.Select(ps => (ps.Packed, ps.BoardSize)));
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);

    public static void RunUniqueUnifiedStatic(
        int boardSize,
        bool parallel,
        int cap,
        Action<int[]>? onMaterialized,
        Action<ulong> onCounted,
        Action<double> reportProgress,
        Func<bool> capReached,
        bool aggressiveSymmetry = false)
    {
        // New path: use fundamental enumeration without global HashSet when parallel
        ulong uniqueCount = 0;
        if (parallel && boardSize > 1)
        {
            ulong countFromEngine = Engines.FundamentalUniqueEnumerationEngine.Enumerate(boardSize, cap, rows =>
            {
                if (onMaterialized != null && (cap <= 0 || capReached() == false))
                {
                    onMaterialized(rows);
                }
            });
            uniqueCount = countFromEngine;
            reportProgress(100.0);
        }
        else
        {
            // Fallback sequential: reuse same engine (Parallel.For will just run fr loops sequentially for small N)
            ulong countFromEngine = Engines.FundamentalUniqueEnumerationEngine.Enumerate(boardSize, cap, rows =>
            {
                if (onMaterialized != null && (cap <= 0 || capReached() == false))
                    onMaterialized(rows);
            });
            uniqueCount = countFromEngine;
            reportProgress(100.0);
        }
        onCounted(uniqueCount);
    }
}
