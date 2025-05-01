namespace NQueen.GUI.Utils;

public class NumericUtility
{
    public static string UpdateMemoryUsage()
    {
        var currentProcess = Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var roundedMemoryUsageInMB = RoundToNearestTen(memoryUsageInBytes / MB);

        return FormatWithSpaceSeparator(roundedMemoryUsageInMB, 0);
    }

    public static string IncrementFormattedNumber(string formattedNumber)
    {
        if (string.IsNullOrWhiteSpace(formattedNumber))
            throw new ArgumentException("Input cannot be null or empty.",
                nameof(formattedNumber));

        var parsedNumber = ParseFormattedNumber(formattedNumber);
        return FormatWithSpaceSeparator(parsedNumber + 1, 0);
    }

    public static int ParseFormattedNumber(string formattedNumber)
    {
        if (string.IsNullOrWhiteSpace(formattedNumber))
            throw new ArgumentException("Input cannot be null or empty.", nameof(formattedNumber));

        var numberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalSeparator = "."
        };

        return int.Parse(formattedNumber.Replace('\u00A0', ' '),
            NumberStyles.Number, numberFormat);
    }

    private static string FormatWithSpaceSeparator(double value, int decimalPlaces)
    {
        var numberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalDigits = decimalPlaces
        };

        return value.ToString("N", numberFormat);
    }

    private static double RoundToNearestTen(double value) =>
        Math.Round(value / 10) * 10;

    private const double MB = 1024.0 * 1024.0;
}
