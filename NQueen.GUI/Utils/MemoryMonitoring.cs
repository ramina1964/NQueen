namespace NQueen.GUI.Utils;

public class MemoryMonitoring
{
    public static string UpdateMemoryUsage()
    {
        const double MB = 1024.0 * 1024;
        const double GB = MB * 1024;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var memoryUsageInMB = memoryUsageInBytes / MB;
        var memoryUsageInGB = memoryUsageInBytes / GB;

        return memoryUsageInGB >= 1
            ? $"{memoryUsageInGB:F2} GB"
            : $"{memoryUsageInMB:F2} MB";
    }
}
