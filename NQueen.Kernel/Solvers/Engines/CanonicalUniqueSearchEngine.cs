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
            int[] scratch = new int[N * 8];
            ulong count = 0;

            void DFS(int col, ulong cols, ulong d1, ulong d2)
            {
                if (col == N)
                {
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
                            var copy = new int[N];
                            Buffer.BlockCopy(queenRows, 0, copy, 0, N * sizeof(int));
                            onSolution(copy);
                        }
                    }
                    return;
                }

                ulong avail = ~(cols | d1 | d2) & fullMask;
                for (int row = 0; row < N; row++)
                {
                    ulong bit = 1UL << row;
                    if ((avail & bit) == 0) continue;
                    queenRows[col] = row;

                    // Early prefix minimality / reflection pruning (opts #1/#14)
                    if (ShouldPrunePrefixFast(queenRows, col, N))
                    {
                        queenRows[col] = -1;
                        continue;
                    }

                    DFS(col + 1, cols | bit, (d1 | bit) << 1, (d2 | bit) >> 1);
                    queenRows[col] = -1;
                }
            }

            DFS(0, 0, 0, 0);
            return count;
        }

        private static bool ShouldPrunePrefixFast(int[] rows, int depth, int N)
        {
            if (!SearchOptimizations.ReflectionPrefixPruningEnabled && !SearchOptimizations.PrefixMinimalityPruningEnabled)
                return false;

            // Reflection pruning: compare prefix with its horizontal reflection
            if (SearchOptimizations.ReflectionPrefixPruningEnabled)
            {
                for (int i = 0; i <= depth; i++)
                {
                    int r = rows[i]; if (r < 0) return false; // incomplete prefix, cannot decide
                    int reflected = N - 1 - r;
                    if (r > reflected) return true; // prefix lexicographically greater than reflection
                    if (r < reflected) break; // strictly less, keep branch
                }
            }
            if (!SearchOptimizations.PrefixMinimalityPruningEnabled) return false;
            // Rotate180 heuristic minimality: compare prefix rows[i] vs transformed of reversed prefix
            for (int i = 0; i <= depth; i++)
            {
                int a = rows[i]; if (a < 0) return false;
                int b = rows[depth - i]; if (b < 0) return false;
                int transformed = N - 1 - b;
                if (a > transformed) return true;
                if (a < transformed) break;
            }
            return false;
        }
    }
}

