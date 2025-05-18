namespace NQueen.Shared.Utils;

public static class ParsingUtils
{
    public static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, out result);

    public static int ParseIntOrThrow(string value) =>
        int.TryParse(value, out int result)
            ? result
            : throw new InvalidOperationException($"The value '{value}' is not a valid integer.");
}
