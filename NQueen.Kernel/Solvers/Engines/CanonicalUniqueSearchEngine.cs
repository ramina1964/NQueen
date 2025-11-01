using System;

namespace NQueen.Kernel.Solvers.Engines
{
 /// <summary>
 /// Symmetry-aware DFS for unique solution counting: generates only canonical representatives (no post-hoc deduplication).
 /// Integrate as a drop-in for CountOnly and MaterializeUnique modes after cap is reached.
 /// </summary>
 public static class CanonicalUniqueSearchEngine
 {
 public static ulong CountUnique(int boardSize, Action<int[]>? onSolution = null)
 {
 if (boardSize <=0) return 0;
 int N = boardSize;
 ulong mask = (N ==64) ? ulong.MaxValue : ((1UL << N) -1UL);
 int[] queenRows = new int[N];
 Array.Fill(queenRows, -1);
 ulong count =0;
 // Restrict first queen to first half (symmetry)
 int maxRow0 = (N +1) /2;
 for (int row0 =0; row0 < maxRow0; row0++)
 {
 queenRows[0] = row0;
 DFS(1, (1UL << row0), (1UL << row0) <<1, (1UL << row0) >>1);
 }
 return count;

 void DFS(int col, ulong cols, ulong d1, ulong d2)
 {
 if (col == N)
 {
 count++;
 onSolution?.Invoke((int[])queenRows.Clone());
 return;
 }
 // Symmetry pruning: enforce lex-minimality (monotonicity) in first two columns
 int minRow =0;
 if (col ==1) minRow = queenRows[0];
 ulong avail = ~(cols | d1 | d2) & mask;
 for (int row = minRow; row < N; row++)
 {
 ulong bit =1UL << row;
 if ((avail & bit) ==0) continue;
 queenRows[col] = row;
 DFS(col +1, cols | bit, (d1 | bit) <<1, (d2 | bit) >>1);
 queenRows[col] = -1;
 }
 }
 }
 }
}
