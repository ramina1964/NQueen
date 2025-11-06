using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using NQueen.Domain.Utils;

namespace NQueen.Kernel.Solvers.Engines
{
    /// <summary>
    /// Parallel fundamental (unique) N-Queen enumerator.
    /// Enumerates only canonical first-column roots (row &lt;= center) and performs
    /// a canonical minimality test at leaves. Does not store global keys; relies on
    /// canonical first-row restriction + minimality filter to count each fundamental solution once.
    /// Materializes up to a provided cap, then counts only.
    /// </summary>
    public static class FundamentalUniqueEnumerationEngine
    {
        public static ulong Enumerate(int boardSize, int materializeCap, Action<int[]>? onCanonicalSolution)
        {
            if (boardSize <= 0) return 0UL;
            int N = boardSize;
            int firstRowLimitExclusive = (N + 1) / 2; // include center when odd
            ulong globalCount = 0;
            int cap = materializeCap <= 0 ? int.MaxValue : materializeCap;
            int materialized = 0;

            Parallel.For(0, firstRowLimitExclusive, fr =>
            {
                // Thread-local buffers
                int[] queenRows = new int[N];
                Array.Fill(queenRows, -1);
                int[] scratch = new int[N * 8];
                ulong fullMask = (N == 64) ? ulong.MaxValue : ((1UL << N) - 1UL);
                ulong[] stackCols = new ulong[N];
                ulong[] stackD1 = new ulong[N];
                ulong[] stackD2 = new ulong[N];
                ulong[] stackAvail = new ulong[N];

                ulong bitFirst = 1UL << fr;
                ulong cols = bitFirst;
                ulong d1 = bitFirst << 1;
                ulong d2 = bitFirst >> 1;
                queenRows[0] = fr;
                int col = 1;
                ulong avail = ~(cols | d1 | d2) & fullMask;

                while (true)
                {
                    if (col == N)
                    {
                        if (SymmetryHelper.IsIdentityCanonical(queenRows, scratch))
                        {
                            Interlocked.Increment(ref Unsafe.As<ulong, long>(ref globalCount));
                            int currentMat = materialized; // fast path check
                            if (currentMat < cap && onCanonicalSolution != null)
                            {
                                int newVal = Interlocked.Increment(ref materialized);
                                if (newVal <= cap)
                                {
                                    var copy = new int[N];
                                    Buffer.BlockCopy(queenRows, 0, copy, 0, N * sizeof(int));
                                    onCanonicalSolution(copy);
                                }
                            }
                        }
                        col--;
                        if (col <= 0) break;
                        Restore(col, out avail, ref cols, ref d1, ref d2, queenRows, stackAvail, stackCols, stackD1, stackD2);
                        continue;
                    }
                    if (avail == 0UL)
                    {
                        col--;
                        if (col <= 0) break;
                        Restore(col, out avail, ref cols, ref d1, ref d2, queenRows, stackAvail, stackCols, stackD1, stackD2);
                        continue;
                    }
                    ulong bit = avail & (ulong)-(long)avail;
                    avail ^= bit;
                    int row = BitOperations.TrailingZeroCount(bit);
                    queenRows[col] = row;
                    stackCols[col] = cols;
                    stackD1[col] = d1;
                    stackD2[col] = d2;
                    stackAvail[col] = avail;
                    cols |= bit;
                    d1 = (d1 | bit) << 1;
                    d2 = (d2 | bit) >> 1;
                    col++;
                    if (col == N) continue;
                    avail = ~(cols | d1 | d2) & fullMask;
                }
            });

            return globalCount;
        }

        private static void Restore(int c, out ulong avail, ref ulong cols, ref ulong d1, ref ulong d2,
            int[] queenRows, ulong[] stackAvail, ulong[] stackCols, ulong[] stackD1, ulong[] stackD2)
        {
            avail = stackAvail[c];
            cols = stackCols[c];
            d1 = stackD1[c];
            d2 = stackD2[c];
            queenRows[c] = -1;
        }
    }
}
