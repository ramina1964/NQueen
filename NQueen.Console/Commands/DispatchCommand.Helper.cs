namespace NQueen.ConsoleApp.Commands;

public partial class DispatchCommands
{
    [GeneratedRegex(@"\s+")]
    private static partial Regex CreateWhiteSpacesRegEx();

    public static Regex RegexSpaces() => CreateWhiteSpacesRegEx();
}