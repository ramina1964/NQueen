namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    public readonly record struct AllRequest(
        int BoardSize, bool EnableEvents, int RootSplitDepth, Action<int[]> OnSolution,
        Action<double> ReportProgress);

    // Added ShouldMaterialize predicate to avoid unnecessary row array allocations when cap reached.
    public readonly record struct UniqueRequest(
        int BoardSize, bool EnableEvents, int RootSplitDepth, Func<bool> ShouldMaterialize,
        Action<int[]> OnUniqueSolution, Action<double> ReportProgress);

    public readonly record struct AllCountOnlyRequest(
        int BoardSize, int RootSplitDepth, Action<ulong> OnCount,
        Action<double> ReportProgress);

    public readonly record struct UniqueCountOnlyRequest(
        int BoardSize, int RootSplitDepth, Action<ulong> OnCount,
        Action<double> ReportProgress);

    private static void ReportRootProgress(
        int done, int total, bool throttle, int bucketSize, ref int lastPercentReported,
        Action<double> report)
    {
        if (!throttle)
        {
            double pctFine = Math.Min(100.0, (double)done / total * 100.0); report(pctFine);
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

    private readonly record struct RootFrame(
        int Col, ulong Cols, ulong D1, ulong D2, int[] Rows);
}
