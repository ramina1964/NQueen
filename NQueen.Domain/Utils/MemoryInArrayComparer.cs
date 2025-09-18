namespace NQueen.Domain.Utils;

public readonly struct MemoryIntArrayComparer :
    IEqualityComparer<Memory<int>>, IComparer<Memory<int>>
{
    // Reusable instance to avoid allocations where a comparer instance is repeatedly requested
    public static readonly MemoryIntArrayComparer Instance = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Memory<int> x, Memory<int> y)
    {
        if (x.Length != y.Length)
            return false;

        return x.Span.SequenceEqual(y.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(Memory<int> obj)
    {
        var span = obj.Span;
        if (span.IsEmpty) return 0;

        var hash = new HashCode();
        foreach (var v in span)
            hash.Add(v);

        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(Memory<int> x, Memory<int> y)
    {
        var spanX = x.Span;
        var spanY = y.Span;
        int len = Math.Min(spanX.Length, spanY.Length);
        for (int i = 0; i < len; i++)
        {
            int a = spanX[i];
            int b = spanY[i];
            if (a != b) return a.CompareTo(b);
        }
        return spanX.Length.CompareTo(spanY.Length);
    }
}
