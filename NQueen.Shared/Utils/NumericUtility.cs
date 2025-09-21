namespace NQueen.Shared.Utils;

public class NumericUtils
{
    public static string UpdateMemoryUsage()
    {
        var currentProcess = Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var roundedMemoryUsageInMB = RoundToNearestTen(memoryUsageInBytes / _megaByte);

        return FormatWithSpaceSeparator(roundedMemoryUsageInMB, 0);
    }

    public static string IncFormattedNumber(string formattedNumber)
    {
        if (string.IsNullOrWhiteSpace(formattedNumber))
            throw new ArgumentException("Input cannot be null or empty.",
                nameof(formattedNumber));

        var parsedNumber = ParseFormattedNumber(formattedNumber);
        return FormatWithSpaceSeparator(parsedNumber, 0);
    }

    public static int ParseFormattedNumber(string formattedNumber)
    {
        if (string.IsNullOrWhiteSpace(formattedNumber))
            throw new ArgumentException("Input cannot be null or empty.", nameof(formattedNumber));

        // Use the current culture's number format for parsing
        var numberFormat = CultureInfo.CurrentCulture.NumberFormat;

        return int.Parse(formattedNumber, NumberStyles.Number, numberFormat);
    }

    public static string FormatWithSpaceSeparator(long value)
    {
        var numberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = _thousandSeparator,
            NumberDecimalDigits = 0
        };

        return value.ToString("N0", numberFormat);
    }

    public static string FormatWithSpaceSeparator(double value, int decimalPlaces = 2)
    {
        var numberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = _thousandSeparator,
            NumberDecimalDigits = decimalPlaces
        };

        return value.ToString("N", numberFormat);
    }

    private static double RoundToNearestTen(double value) =>
        Math.Round(value / 10) * 10;

    private const double _megaByte = 1024.0 * 1024.0;
    private const string _thousandSeparator = " ";
}
