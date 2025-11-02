using System.Linq;
namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    private const int AggressiveSymmetryThreshold = 12;

    // Helper for HashSet capacity estimation (powers of two above expected unique count)
    private static int EstimateUniqueCapacity(int n)
    {
        // Lower bound: n=8?12, n=10?40, n=12?92, n=14?365, n=16?1477
        // Use 2x known unique count for safety, or 1<<n for larger n
        return n switch
        {
            8 => 32,
            10 => 128,
            12 => 256,
            14 => 1024,
            16 => 4096,
            _ => 1 << (n > 20 ? 20 : n) // up to 1M for n>=20
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

    // Shared unique solution search core
    private void RunUniqueUnified(bool parallel)
    {
        int N = BoardSize;
        int cap = _capEnabled ? _maxDisplayedCount : int.MaxValue;
        _solutions.Clear(); _rawSolutions = null; _eventsSuppressedAfterCap = false; _solutionCount =0;
        var rawSample = new List<int[]>();
        var packedSample = new List<PackedSolution>();
        int materialized =0;
        int capReachedFlag =0; //0 = not reached,1 = reached
        object lockObj = new object();

        // Use CanonicalUniqueSearchEngine for unique solution enumeration
        ulong uniqueCount = CanonicalUniqueSearchEngine.CountUnique(N, rows =>
        {
            if (System.Threading.Volatile.Read(ref capReachedFlag) ==1) return;
            if (materialized < Math.Max(1, cap))
            {
                var storedCopy = new int[N];
                Array.Copy(rows, storedCopy, N);
                rawSample.Add(storedCopy);
                var packed = N <=25 ? SymmetryHelper.PackCanonical(rows, N) :0;
                packedSample.Add(new PackedSolution(packed, N));
                materialized++;
                if (materialized >= cap && _capEnabled)
                {
                    _eventsSuppressedAfterCap = true;
                    System.Threading.Volatile.Write(ref capReachedFlag,1);
                }
            }
        });
        _solutionCount = uniqueCount;
        _rawSolutions = rawSample;
        // Convert PackedSolution to tuple for _solutions
        _solutions.AddRange(packedSample.Select(ps => (ps.Packed, ps.BoardSize)));
        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    private void RunUniqueParallel() => RunUniqueUnified(parallel: true);
    private void RunUniqueSequential() => RunUniqueUnified(parallel: false);

    // Static unified unique enumeration for both materialize and count-only
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
        ulong uniqueCount =0;
        if (parallel && boardSize >1)
        {
            ulong countFromEngine =0;
            BitmaskParallelEngine.RunUniqueUnified(
                boardSize,
                enableEvents: false,
                cap: cap,
                onUniqueSolution: onMaterialized ?? (_ => { }),
                onCompletedUniqueCount: c => countFromEngine = c,
                reportProgress: reportProgress,
                capReached: capReached
            );
            uniqueCount = countFromEngine;
        }
        else
        {
            var uniqueKeys = new HashSet<UInt128>(EstimateUniqueCapacity(boardSize));
            uniqueKeys.EnsureCapacity(EstimateUniqueCapacity(boardSize));
            int[] scratch = new int[SymmetryHelper.GetScratchBufferSize(boardSize)];
            int materialized =0;
            for (int root =0; root < boardSize && (cap <=0 || materialized < cap); root++)
            {
                BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                    boardSize,
                    RestrictFirstCol: true,
                    EnhancedSymmetry: true,
                    AggressiveSymmetry: aggressiveSymmetry,
                    DisplayMode.Hide,
                    DelayInMillisec:0,
                    SimulationToken: Guid.Empty,
                    IsCanceled: () => false,
                    ReportProgress: reportProgress,
                    OnQueenPlaced: _ => { },
                    OnSolution: rows =>
                    {
                        if (boardSize <=25)
                        {
                            UInt128 fastKey = SymmetryHelper.PackCanonical(rows, boardSize);
                            if (uniqueKeys.Contains(fastKey))
                                return false;
                        }
                        if (!uniqueKeys.Add(SymmetryHelper.PackCanonical(SymmetryHelper.GetCanonicalForm(rows, scratch, null), rows.Length)))
                            return false;
                        if (cap >0 && materialized < cap && onMaterialized != null)
                        {
                            onMaterialized((int[])rows.Clone());
                            materialized++;
                        }
                        return false;
                    }
                ));
            }
            uniqueCount = (ulong)uniqueKeys.Count;
        }
        onCounted(uniqueCount);
    }
}
