namespace NQueen.Domain.Utils;

public class IntArrayComparer : IEqualityComparer<int[]>
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
}
