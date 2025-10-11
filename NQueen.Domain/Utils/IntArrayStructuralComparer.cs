namespace NQueen.Domain.Utils;

/// <summary>
/// Structural equality comparer for int[] using Span-based comparison.
/// Centralized to avoid duplicated anonymous / nested comparer implementations across projects.
/// </summary>
public sealed class IntArrayStructuralComparer : IEqualityComparer<int[]>
{
    public static readonly IntArrayStructuralComparer Instance = new();

    private IntArrayStructuralComparer() { }

    public bool Equals(int[]? x, int[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Length != y.Length) return false;
        return x.AsSpan().SequenceEqual(y);
    }

    public int GetHashCode(int[] obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        unchecked
        {
            int hash = 17;
            foreach (var v in obj)
                hash = hash * 31 + v;
            return hash;
        }
    }
}
