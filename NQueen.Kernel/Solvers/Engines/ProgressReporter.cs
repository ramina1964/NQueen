namespace NQueen.Kernel.Solvers.Engines;

internal readonly struct ProgressReporter
{
    private readonly Action<double> _report;
    private readonly int _bucketSize;
    private readonly Stopwatch _heartbeat;
    private readonly int _heartbeatMs;

    public ProgressReporter(Action<double> report, int bucketSize = 1, int heartbeatMs = 1500)
    {
        _report = report;
        _bucketSize = bucketSize;
        _heartbeat = Stopwatch.StartNew();
        _heartbeatMs = heartbeatMs;
    }

    public void ReportBucket(int done, int totalTasks, ref int bucketReported)
    {
        double pct = totalTasks == 0 ? 100.0 : (double)done / totalTasks * 100.0;
        int bucket = (int)pct / _bucketSize * _bucketSize;
        int observed;
        while (bucket > (observed = Volatile.Read(ref bucketReported)))
        {
            if (Interlocked.CompareExchange(ref bucketReported, bucket, observed) == observed)
            {
                _report(bucket);
                break;
            }
        }
        if (_heartbeat.ElapsedMilliseconds >= _heartbeatMs)
        {
            _report(Math.Min(99.0, pct));
            _heartbeat.Restart();
        }
    }
}
