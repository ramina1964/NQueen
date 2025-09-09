namespace NQueen.Domain.Utils;

public readonly struct IntArrayComparer : IEqualityComparer<int[]>, IComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Length != y.Length) return false;

        // Compare only the first, middle, and last elements for early exit
        if (x[0] != y[0] || x[^1] != y[^1] || x[x.Length / 2] != y[y.Length / 2])
            return false;

        // Fallback to full comparison
        for (int i = 0; i < x.Length; i++)
            if (x[i] != y[i]) return false;

        return true;
    }

    public int GetHashCode(int[] obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        unchecked
        {
            int hash = 17;
            foreach (var item in obj)
            {
                hash = hash * 31 + item.GetHashCode();
            }
            return hash;
        }
    }

    public int Compare(int[]? x, int[]? y)
    {
        ArgumentNullException.ThrowIfNull(x, nameof(x));
        ArgumentNullException.ThrowIfNull(y, nameof(y));

        int length = Math.Min(x.Length, y.Length);
        for (int i = 0; i < length; i++)
        {
            int comparison = x[i].CompareTo(y[i]);
            if (comparison != 0) return comparison;
        }

        return x.Length.CompareTo(y.Length);
    }

    // Custom hash function for arrays
    private static int ComputeArrayHash(int[] array)
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in array)
            {
                hash = hash * 31 + item;
            }
            return hash;
        }
    }
}
