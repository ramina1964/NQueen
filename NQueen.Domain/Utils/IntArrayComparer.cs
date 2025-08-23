namespace NQueen.Domain.Utils;

public readonly struct IntArrayComparer : IEqualityComparer<int[]>, IComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        if (x.Length != y.Length)
            return false;

        return x.AsSpan().SequenceEqual(y.AsSpan());
    }

    public int GetHashCode(int[] obj)
    {
        if (obj is null) return 0;

        unchecked
        {
            var hash = 17;

            // Combine the first few elements
            var takeCount = Math.Min(3, obj.Length);
            for (var i = 0; i < takeCount; i++)
                hash = hash * 31 + obj[i];

            // Combine the last few elements
            var skipCount = Math.Max(0, obj.Length - 3);
            for (var i = skipCount; i < obj.Length; i++)
                hash = hash * 31 + obj[i];

            return hash;
        }
    }

    public int Compare(int[]? x, int[]? y)
    {
        ArgumentNullException.ThrowIfNull(x, nameof(x));
        ArgumentNullException.ThrowIfNull(y, nameof(y));

        var length = Math.Min(x.Length, y.Length);
        for (var i = 0; i < length; i++)
        {
            var comparison = x[i].CompareTo(y[i]);
            if (comparison != 0) return comparison;
        }

        return x.Length.CompareTo(y.Length);
    }
}
