namespace NQueen.Kernel.Solvers;

public partial class BitmaskSolver
{
    // Materialises up to `cap` sample solutions when the count was served from the lookup table.
    // For large boards (>= ConstructiveSampleThresholdN) uses a constructive algorithm;
    // for smaller boards runs a capped BitmaskSearchEngine pass.
    private void SampleMaterializeUsingLookup(bool isUnique)
    {
        int cap = _maxDisplayedCount;
        if (cap <= 0) return;

        if (BoardSize >= SimulationSettings.ConstructiveSampleThresholdN)
        {
            ConstructiveSampleSolutions(isUnique, cap);
            ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));
            return;
        }

        if (isUnique)
        {
            var seen = new HashSet<UInt128>();
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p =>
                {
                    if (EnableEvents)
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken));
                },
                m =>
                {
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize));
                },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    UInt128 packed = 0;
                    if (rows.Length <= 25)
                        packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                    if (seen.Add(packed))
                    {
                        AddSample(rows);
                        if (seen.Count >= cap)
                        {
                            _eventsSuppressedAfterCap = true;
                            return true;
                        }
                    }
                    return false;
                }
            ));
        }
        else
        {
            int materializedSamples = 0;
            BitmaskSearchEngine.Run(new BitmaskSearchEngine.Request(
                BoardSize,
                RestrictFirstCol: false,
                EnhancedSymmetry: false,
                AggressiveSymmetry: false,
                CountOnly: false,
                DisplayMode,
                DelayInMillisec,
                _currentSimToken,
                () => IsSolverCanceled,
                p =>
                {
                    if (EnableEvents)
                        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(p, _currentSimToken));
                },
                m =>
                {
                    if (EnableEvents && !_eventsSuppressedAfterCap)
                        QueenPlaced?.Invoke(this, new QueenPlacedEventArgs(m, BoardSize));
                },
                rows =>
                {
                    if (!ValidateRows(rows)) return false;
                    if (materializedSamples < cap)
                    {
                        AddSample(rows);
                        materializedSamples++;
                        if (materializedSamples >= cap)
                        {
                            _eventsSuppressedAfterCap = true;
                            return true;
                        }
                    }
                    return false;
                }
            ));
        }

        ProgressValueChanged?.Invoke(this, new ProgressUpdateEventArgs(100.0, _currentSimToken));

        void AddSample(int[] rows)
        {
            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                _solutions.Add((packed, rows.Length));
            }
            else
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
            }

            if (EnableEvents && !_eventsSuppressedAfterCap)
                SolutionFound?.Invoke(this, new SolutionFoundEventArgs(new Memory<int>(rows), BoardSize));
        }
    }

    // Produces up to `cap` sample solutions constructively (no DFS) using the explicit
    // construction algorithm and its symmetry variants. Used for very large boards where
    // running a DFS just to get a handful of samples would be prohibitively slow.
    private void ConstructiveSampleSolutions(bool isUnique, int cap)
    {
        if (cap <= 0) return;

        var baseRows = GenerateConstructiveSolution(BoardSize);
        if (!ValidateRows(baseRows)) return;

        AddMaterialized(baseRows);
        if (cap == 1) return;

        int remaining = cap - 1;
        var variants = GenerateSymmetryVariants(baseRows, remaining);
        foreach (var v in variants)
            AddMaterialized(v);

        void AddMaterialized(int[] rows)
        {
            if (_solutions.Count + _largeBoardRawSolutions.Count >= cap) return;

            if (isUnique)
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
                return;
            }

            if (rows.Length <= 25)
            {
                var packed = SymmetryHelper.GetCanonicalKey(rows, _scratchBuffer!, out _);
                _solutions.Add((packed, rows.Length));
            }
            else
            {
                var copy = new int[rows.Length];
                Array.Copy(rows, copy, rows.Length);
                _largeBoardRawSolutions.Add(copy);
            }
        }
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

    // Generates up to `maxVariants` symmetry variants (rotations + reflections) of a base solution.
    private static List<int[]> GenerateSymmetryVariants(int[] rows, int maxVariants)
    {
        var list = new List<int[]>(Math.Min(maxVariants, 7));
        void AddVariant(int[] r) { if (list.Count < maxVariants) list.Add(r); }

        int n = rows.Length;

        int[] Rotate90(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
            {
                int oldRow = src[c];
                int newCol = oldRow;
                int newRow = n - 1 - c;
                r[newCol] = newRow;
            }
            return r;
        }

        int[] ReflectVertical(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
                r[n - 1 - c] = src[c];
            return r;
        }

        int[] ReflectHorizontal(int[] src)
        {
            var r = new int[n];
            for (int c = 0; c < n; c++)
                r[c] = n - 1 - src[c];
            return r;
        }

        var r90  = Rotate90(rows);        AddVariant(r90);
        var r180 = Rotate90(r90);         AddVariant(r180);
        var r270 = Rotate90(r180);        AddVariant(r270);
        var vref = ReflectVertical(rows); AddVariant(vref);
        var href = ReflectHorizontal(rows); AddVariant(href);
        var diag = ReflectVertical(r90);  AddVariant(diag);

        return list;
    }
}
