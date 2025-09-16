namespace NQueen.Domain.Utils;

public readonly struct IntArrayComparer : IEqualityComparer<int[]>, IComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Length != y.Length) return false;

        // Full comparison of all elements
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i]) return false;
        }

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
