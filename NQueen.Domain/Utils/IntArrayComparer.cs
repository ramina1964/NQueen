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
        {
            if (x[i] != y[i]) return false;
        }
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        if (obj is null) return 0;

        unchecked
        {
            var hash = 17;

            // Combine the first, middle, and last elements
            if (obj.Length > 0) hash = hash * 31 + obj[0];
            if (obj.Length > 1) hash = hash * 31 + obj[obj.Length / 2];
            if (obj.Length > 2) hash = hash * 31 + obj[^1];

            // Optionally include more elements for better distribution
            if (obj.Length > 3) hash = hash * 31 + obj[1];
            if (obj.Length > 4) hash = hash * 31 + obj[obj.Length - 2];

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
}
