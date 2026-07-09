namespace NQueen.UnitTests.Tests.Solver;

/// <summary>
/// Edge-case coverage for the internal ProgressReporter bucket logic. The heartbeat
/// branch is time-based (default 1500 ms); these tests pass a large heartbeatMs so the
/// deterministic bucket-crossing behavior can be asserted in isolation.
/// </summary>
[Trait("Category", "Solver")]
[Trait("Behavior", "Progress")]
public class ProgressReporterTests
{
    private const int NoHeartbeat = int.MaxValue;

    [Fact]
    public void ReportBucket_ZeroTotalTasks_ReportsOneHundred()
    {
        var reported = new List<double>();
        var reporter = new ProgressReporter(reported.Add, bucketSize: 1, heartbeatMs: NoHeartbeat);
        int bucketReported = 0;

        reporter.ReportBucket(done: 0, totalTasks: 0, ref bucketReported);

        reported.ShouldHaveSingleItem();
        reported[0].ShouldBe(100.0);
    }

    [Fact]
    public void ReportBucket_CrossingBucketBoundary_ReportsBucketedPercent()
    {
        var reported = new List<double>();
        var reporter = new ProgressReporter(reported.Add, bucketSize: 10, heartbeatMs: NoHeartbeat);
        int bucketReported = 0;

        // 3/10 => 30% => bucket 30
        reporter.ReportBucket(done: 3, totalTasks: 10, ref bucketReported);

        reported.ShouldHaveSingleItem();
        reported[0].ShouldBe(30.0);
        bucketReported.ShouldBe(30);
    }

    [Fact]
    public void ReportBucket_WithinSameBucket_DoesNotReportAgain()
    {
        var reported = new List<double>();
        var reporter = new ProgressReporter(reported.Add, bucketSize: 10, heartbeatMs: NoHeartbeat);
        int bucketReported = 0;

        // 3/10 = 30% and 3.5/10-equivalent 35% both fall in bucket 30 => only one report.
        reporter.ReportBucket(done: 3, totalTasks: 10, ref bucketReported); // 30% -> bucket 30
        reporter.ReportBucket(done: 35, totalTasks: 100, ref bucketReported); // 35% -> still bucket 30

        reported.ShouldHaveSingleItem();
        bucketReported.ShouldBe(30);
    }

    [Fact]
    public void ReportBucket_AdvancingBuckets_ReportsEachNewBucketOnce()
    {
        var reported = new List<double>();
        var reporter = new ProgressReporter(reported.Add, bucketSize: 25, heartbeatMs: NoHeartbeat);
        int bucketReported = 0;

        reporter.ReportBucket(done: 25, totalTasks: 100, ref bucketReported);  // 25 -> bucket 25
        reporter.ReportBucket(done: 60, totalTasks: 100, ref bucketReported);  // 60 -> bucket 50
        reporter.ReportBucket(done: 100, totalTasks: 100, ref bucketReported); // 100 -> bucket 100

        reported.ShouldBe([25.0, 50.0, 100.0]);
        bucketReported.ShouldBe(100);
    }

    [Fact]
    public void ReportBucket_BucketNeverGoesBackwards()
    {
        var reported = new List<double>();
        var reporter = new ProgressReporter(reported.Add, bucketSize: 10, heartbeatMs: NoHeartbeat);
        int bucketReported = 0;

        reporter.ReportBucket(done: 8, totalTasks: 10, ref bucketReported); // 80 -> bucket 80
        reporter.ReportBucket(done: 2, totalTasks: 10, ref bucketReported); // 20 -> lower, no report

        reported.ShouldHaveSingleItem();
        reported[0].ShouldBe(80.0);
        bucketReported.ShouldBe(80);
    }
}
