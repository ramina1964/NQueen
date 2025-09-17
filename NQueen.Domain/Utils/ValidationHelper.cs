namespace NQueen.Domain.Utils;

public static class ValidationHelper
{
    public static bool AreAllPositionsValid(Span<int> positions)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            if (positions[i] < 0)
                return false;
        }

        return true;
    }
}
