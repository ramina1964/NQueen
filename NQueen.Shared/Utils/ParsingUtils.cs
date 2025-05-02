namespace NQueen.Shared.Utils;

public static class ParsingUtils
{
    /// <summary>
    /// Tries to parse a string into an integer.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="result">The parsed integer, if successful.</param>
    /// <returns>True if parsing was successful; otherwise, false.</returns>
    public static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, out result);

    /// <summary>
    /// Parses a string into an integer and throws an exception if invalid.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed integer.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the string is not a valid integer.</exception>
    public static int ParseIntOrThrow(string value) =>
        int.TryParse(value, out int result)
            ? result
            : throw new InvalidOperationException($"The value '{value}' is not a valid integer.");
}
