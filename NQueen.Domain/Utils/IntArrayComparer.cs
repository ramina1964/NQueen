namespace NQueen.Domain.Utils;

public class IntArrayComparer : IEqualityComparer<int[]>, IComparer<int[]>
{
    public bool Equals(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x.Length != y.Length) return false;
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
            int hash = 17;
            foreach (var item in obj)
            {
                hash = hash * 31 + item;
            }
            return hash;
        }
    }

    public int Compare(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int lengthComparison = x.Length.CompareTo(y.Length);
        if (lengthComparison != 0) return lengthComparison;

        for (int i = 0; i < x.Length; i++)
        {
            int elementComparison = x[i].CompareTo(y[i]);
            if (elementComparison != 0) return elementComparison;
        }

        return 0;
    }
}
