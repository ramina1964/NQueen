namespace NQueen.Domain.Utils;

public readonly struct MemoryIntArrayComparer :
    IEqualityComparer<Memory<int>>, IComparer<Memory<int>>
{
    public bool Equals(Memory<int> x, Memory<int> y)
    {
        if (x.Length != y.Length) return false;

        var spanX = x.Span;
        var spanY = y.Span;

        // Compare elements one by one
        for (int i = 0; i < spanX.Length; i++)
        {
            if (spanX[i] != spanY[i]) return false;
        }

        return true;
    }

    public int GetHashCode(Memory<int> obj)
    {
        if (obj.IsEmpty) return 0;

        var span = obj.Span;
        unchecked
        {
            int hash = 17;
            foreach (var item in span)
            {
                hash = hash * 31 + item;
            }
            return hash;
        }
    }

    public int Compare(Memory<int> x, Memory<int> y)
    {
        var spanX = x.Span;
        var spanY = y.Span;

        int length = Math.Min(spanX.Length, spanY.Length);
        for (int i = 0; i < length; i++)
        {
            int comparison = spanX[i].CompareTo(spanY[i]);
            if (comparison != 0) return comparison;
        }

        // If all compared elements are equal, compare lengths
        return spanX.Length.CompareTo(spanY.Length);
    }
}
