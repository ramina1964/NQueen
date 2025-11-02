using System;
using NQueen.Domain.Utils;

namespace NQueen.Kernel.Solvers.Engines
{
    /// <summary>
    /// DFS that counts only fundamental (unique) N-Queen solutions under the8 board symmetries.
    /// A solution is counted iff it is the lexicographically minimal representative among all its transforms.
    /// Performs full enumeration (no partial symmetry pruning) to avoid undercounting canonical representatives.
    /// </summary>
    public static class CanonicalUniqueSearchEngine
    {
        public static ulong CountUnique(int boardSize, Action<int[]>? onSolution = null)
        {
            if (boardSize <= 0) return 0;
            int N = boardSize;
            ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
            int[] queenRows = new int[N];
            Array.Fill(queenRows, -1);
            // scratch buffer for canonicalization (needs N*8 per SymmetryHelper contract)
            int[] scratch = new int[N * 8];
            ulong count = 0;

            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
                    // Check canonical minimality
                    var canon = SymmetryHelper.GetCanonicalForm(queenRows, scratch, null);
                    bool isCanonical = true;
                    for (int i = 0; i < N; i++)
                    {
                        if (queenRows[i] != canon[i]) { isCanonical = false; break; }
                    }
                    if (isCanonical)
                    {
                        count++;
                        if (onSolution != null)
                        {
                            // Emit canonical representative (which equals queenRows)
                            var copy = new int[N];
                            Buffer.BlockCopy(queenRows, 0, copy, 0, N * sizeof(int));
                            onSolution(copy);
                        }
                    }
                    return;
                }

                // Compute available rows
                ulong avail = ~(cols | d1 | d2) & fullMask;
                // Removed symmetry pruning to avoid discarding valid canonical representatives.

                for (int row = 0; row < N; row++)
                {
                    ulong bit = 1UL << row;
                    if ((avail & bit) == 0) continue;
                    queenRows[col] = row;
                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    queenRows[col] = -1;
                }
            }

            DFS(0, 0, 0, 0);
            return count;
        }
    }
}

