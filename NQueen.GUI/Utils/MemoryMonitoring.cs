namespace NQueen.GUI.Utils;

public class MemoryMonitoring
{
    public static string UpdateMemoryUsage()
    {
        const double MB = 1024.0 * 1024.0;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var memoryUsageInMB = memoryUsageInBytes / MB;

        return FormatWithSpaceSeparator(memoryUsageInMB);
    }

    private static string FormatWithSpaceSeparator(double value)
    {
        var numberFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = " ",
            NumberDecimalDigits = 2
        };

        return value.ToString("N", numberFormat);
    }
}
