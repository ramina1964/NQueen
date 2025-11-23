namespace NQueen.Kernel.Solvers.Engines;

internal sealed partial class BitmaskParallelEngine
{
    // Primary request for full All mode enumeration (materialize + count)
    public readonly record struct AllRequest(
        int BoardSize, int RootSplitDepth, bool EnableEvents, int MaterializeCap, Action<int[]> OnSolution,
        Action<ulong> OnCompleted, Action<double> ReportProgress);

    // Unique mode request (materialize sample canonical solutions optionally)
    public readonly record struct UniqueRequest(
        int BoardSize, bool EnableEvents, Func<bool> ShouldMaterialize,
        Action<int[]> OnUniqueSolution, Action<ulong> OnCompletedUniqueCount, Action<double> ReportProgress);

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
