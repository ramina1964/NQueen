namespace NQueen.ConsoleApp;

public class ConsoleUtils : IConsoleUtils
{
    public void WriteLineColored(ConsoleColor color, string message)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previousColor;
    }
}
