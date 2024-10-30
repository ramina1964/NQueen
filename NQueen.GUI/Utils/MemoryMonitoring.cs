namespace NQueen.GUI.Utils;

public class MemoryMonitoring
{
    public static string UpdateMemoryUsage()
    {
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var memoryUsageInMB = Math.Round(memoryUsageInBytes / MB);

        return FormatWithSpaceSeparator(memoryUsageInMB, 0);
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

    private const double MB = 1024.0 * 1024.0;
}
