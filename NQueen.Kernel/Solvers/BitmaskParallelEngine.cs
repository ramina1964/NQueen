namespace NQueen.Kernel.Solvers;

internal sealed partial class BitmaskParallelEngine
{
    // Request contracts
    public readonly record struct AllRequest(
        int BoardSize,
        bool EnableEvents,
        int RootSplitDepth,
        Action<int[]> OnSolution,
        Action<double> ReportProgress);

    public readonly record struct UniqueRequest(
        int BoardSize,
        bool EnableEvents,
        int RootSplitDepth,
        Action<int[]> OnUniqueSolution,
        Action<double> ReportProgress);

    public readonly record struct AllCountOnlyRequest(
        int BoardSize,
        int RootSplitDepth,
        Action<ulong> OnCount,
        Action<double> ReportProgress);

    public readonly record struct UniqueCountOnlyRequest(
        int BoardSize,
        int RootSplitDepth,
        Action<ulong> OnCount,
        Action<double> ReportProgress);

    // Shared progress throttling helper
    private static void ReportRootProgress(int done, int total, bool throttle, int bucketSize, ref int lastPercentReported, Action<double> report)
    {
        if (!throttle)
        {
            double pctFine = Math.Min(100.0, (double)done / total * 100.0);
            report(pctFine);
        }
        else
        {
            int pctInt = (int)((double)done * 100 / total);
            int bucket = (pctInt / bucketSize) * bucketSize;
            int observed;
            while (bucket > (observed = Volatile.Read(ref lastPercentReported)))
            {
                if (Interlocked.CompareExchange(ref lastPercentReported, bucket, observed) == observed)
                {
                    report(bucket);
                    break;
                }
            }
        }
    }

    // Root frame used for All-solution expansion
    private readonly record struct RootFrame(
        int Col, ulong Cols, ulong D1, ulong D2, int[] Rows);
}
