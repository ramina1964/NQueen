﻿namespace NQueen.Kernel.Utilities;

public class SequenceEquality<T> : IEqualityComparer<IEnumerable<T>>
{
    // Constant related to hash table
    public const int HashConstant = 37;

    public bool Equals(IEnumerable<T> x, IEnumerable<T> y) => x.SequenceEqual(y);

    public int GetHashCode(IEnumerable<T> seq)
    {
        return seq.Aggregate(1234567, (current, elem) =>
            current * HashConstant + elem.GetHashCode());
    }
}
