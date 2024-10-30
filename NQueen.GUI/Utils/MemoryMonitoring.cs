namespace NQueen.GUI.Utils;

public class MemoryMonitoring
{
    public static string UpdateMemoryUsage()
    {
        const double MB = 1024.0 * 1024.0;
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        var memoryUsageInBytes = currentProcess.WorkingSet64;
        var memoryUsageInGB = memoryUsageInBytes / MB;

        return memoryUsageInGB.ToString("F2");
    }
}
