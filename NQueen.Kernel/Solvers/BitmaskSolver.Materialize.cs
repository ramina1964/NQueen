namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Materialises up to `cap` GENUINELY DISTINCT sample solutions when the count was served
    // from the lookup table (N >= LookupThresholdN). Runs an early-exit DFS that stops the
    // moment `cap` solutions are stored — never a full enumeration. The total solution count
    // is supplied separately by the lookup table in HandleModeCommon, so this method only has
    // to produce the handful of display samples.
    //
    // Unique mode collects canonical representatives (CollectUniqueSamplesDFS verifies identity
    // canonicality, so each sample is a different fundamental solution); All mode collects the
    // first `cap` distinct placements (CollectAllSampleSolutionsDFS). Both reuse the exact
    // collectors already proven on the N = 14..20 materialize paths.
    private void SampleMaterializeUsingLookup(bool isUnique)
    {
        int cap = _maxDisplayedCount;
        if (cap <= 0) return;

        if (isUnique)
        {
            var packedSample = new List<(UInt128 packed, int boardSize)>(cap);
            int materialized = 0;
            CollectUniqueSamplesDFS(BoardSize, Math.Max(1, cap), packedSample, ref materialized);
            _solutions.AddRange(packedSample);
        }
        else
        {
            CollectAllSampleSolutionsDFS(BoardSize, Math.Max(1, cap));
        }

        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
    }

    // Explicit construction: produces one valid N-queens placement without backtracking,
    // following the well-known closed-form solution keyed on n mod 6 (see Hoffman, Loessi
    // & Moore; also Wikipedia, "Eight queens puzzle § Explicit solutions"). The even rows
    // precede the odd rows; the remainders 2 and 3 need a small reordering that would
    // otherwise place two queens on a shared diagonal. For every other remainder this is
    // plain "evens then odds". Valid for all n >= 4.
    private static int[] GenerateConstructiveSolution(int n)
    {
        int rem = n % 6;

        // Even rows 2, 4, ..., (largest even <= n).
        var evens = new List<int>(n / 2 + 1);
        for (int i = 2; i <= n; i += 2) evens.Add(i);

        // Odd rows 1, 3, ..., (largest odd <= n).
        var odds = new List<int>(n / 2 + 1);
        for (int i = 1; i <= n; i += 2) odds.Add(i);

        if (rem == 2)
        {
            // Switch the places of 1 and 3, then move 5 to the end of the odd list.
            if (odds.Count >= 2) (odds[0], odds[1]) = (odds[1], odds[0]);
            if (odds.Remove(5)) odds.Add(5);
        }
        else if (rem == 3)
        {
            // Move 2 to the end of the even list, and 1 then 3 to the end of the odd list.
            if (evens.Remove(2)) evens.Add(2);
            if (odds.Remove(1)) odds.Add(1);
            if (odds.Remove(3)) odds.Add(3);
        }

        var rows = new int[n];
        int col = 0;
        foreach (int r in evens) rows[col++] = r - 1;
        foreach (int r in odds) rows[col++] = r - 1;
        return rows;
    }
}
