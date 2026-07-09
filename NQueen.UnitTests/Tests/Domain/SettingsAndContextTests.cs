using System.Threading;
using System.Threading.Channels;

namespace NQueen.UnitTests.Tests.Domain;

public class SettingsAndContextTests
{
    // ── BoardSettings ─────────────────────────────────────────────────────────

    [Fact]
    public void BoardSettings_HasExpectedConstants()
    {
        BoardSettings.DefaultBoardSize.ShouldBe(8);
        BoardSettings.MinSize.ShouldBe(1);
        BoardSettings.MaxBitmaskBoardSize.ShouldBe(64);
        BoardSettings.MaxSizeForSingle.ShouldBe(37);
        BoardSettings.MaxSizeForUnique.ShouldBe(25);
        BoardSettings.MaxSizeForAll.ShouldBe(25);
        BoardSettings.WhiteQueenChar.ShouldBe('\u2655');
    }

    [Fact]
    public void BoardSettings_QueenImageResource_PointsToGuiPackUri()
    {
        BoardSettings.QueenImageResource.ShouldStartWith("pack://application:,,,/NQueen.GUI");
        BoardSettings.QueenImageResource.ShouldEndWith("WhiteQueen.png");
    }

    [Fact]
    public void BoardSettings_MaintainsSizeInvariants()
    {
        BoardSettings.MinSize.ShouldBeLessThan(BoardSettings.DefaultBoardSize);
        BoardSettings.DefaultBoardSize.ShouldBeLessThanOrEqualTo(BoardSettings.MaxSizeForAll);
        BoardSettings.MaxSizeForUnique.ShouldBe(BoardSettings.MaxSizeForAll);
        BoardSettings.MaxSizeForSingle.ShouldBeGreaterThan(BoardSettings.MaxSizeForUnique);
        BoardSettings.MaxSizeForSingle.ShouldBeLessThanOrEqualTo(BoardSettings.MaxBitmaskBoardSize);
    }

    // ── SimulationSettings ────────────────────────────────────────────────────

    [Fact]
    public void SimulationSettings_HasExpectedDefaults()
    {
        SimulationSettings.MaxDisplayedCount.ShouldBe(5);
        SimulationSettings.DefaultDelayInMilliseconds.ShouldBe(500);
        SimulationSettings.MinDelayInMilliseconds.ShouldBe(5);
        SimulationSettings.DefaultSolutionMode.ShouldBe(SolutionMode.Unique);
        SimulationSettings.DefaultDisplayMode.ShouldBe(DisplayMode.Hide);
        SimulationSettings.DefaultUseParallel.ShouldBeTrue();
        SimulationSettings.DefaultAllStorageMode.ShouldBe(ResultStorageMode.Materialize);
        SimulationSettings.DefaultUniqueStorageMode.ShouldBe(ResultStorageMode.Materialize);
    }

    [Fact]
    public void SimulationSettings_DelayInvariant_MinBelowDefault()
    {
        SimulationSettings.MinDelayInMilliseconds.ShouldBeLessThan(SimulationSettings.DefaultDelayInMilliseconds);
    }

    [Fact]
    public void SimulationSettings_ThresholdsArePositive()
    {
        SimulationSettings.LookupThresholdN.ShouldBeGreaterThan(0);
        SimulationSettings.ParallelAllMaterializeAutoEnableThresholdN.ShouldBeGreaterThan(0);
        SimulationSettings.UniqueCountOnlyParallelThresholdN.ShouldBeGreaterThan(0);
        SimulationSettings.QueenPlacedSamplingThresholdSize.ShouldBeGreaterThan(0);
        SimulationSettings.QueenPlacedLargeBoardSampleRate.ShouldBeGreaterThan(0);
        SimulationSettings.MaxVisualizeBoardSize.ShouldBeGreaterThan(0);
        SimulationSettings.LargeBoardSymmetryPruningThreshold.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SimulationSettings_ProgressThresholdPct_IsMutableWithDefaultFive()
    {
        var original = SimulationSettings.ProgressThresholdPct;
        try
        {
            SimulationSettings.ProgressThresholdPct = 5;
            SimulationSettings.ProgressThresholdPct.ShouldBe(5);

            SimulationSettings.ProgressThresholdPct = 10;
            SimulationSettings.ProgressThresholdPct.ShouldBe(10);
        }
        finally
        {
            SimulationSettings.ProgressThresholdPct = original;
        }
    }

    // ── SimulationContext ─────────────────────────────────────────────────────

    [Fact]
    public void SimulationContext_RequiredArgs_SetProperties()
    {
        var context = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);

        context.BoardSize.ShouldBe(8);
        context.SolutionMode.ShouldBe(SolutionMode.Unique);
        context.DisplayMode.ShouldBe(DisplayMode.Hide);
    }

    [Fact]
    public void SimulationContext_OptionalSinks_DefaultToNullAndDefaultToken()
    {
        var context = new SimulationContext(8, SolutionMode.All, DisplayMode.Visualize);

        context.OnProgress.ShouldBeNull();
        context.OnSolutionFound.ShouldBeNull();
        context.OnQueenPlaced.ShouldBeNull();
        context.Cancellation.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public void SimulationContext_AcceptsProvidedSinksAndToken()
    {
        var progress = new Progress<ProgressInfo>();
        var solutionFound = new Progress<SolutionFoundInfo>();
        var channel = Channel.CreateUnbounded<QueenPlacedInfo>();
        using var cts = new CancellationTokenSource();

        var context = new SimulationContext(
            10, SolutionMode.Single, DisplayMode.Visualize,
            progress, cts.Token, solutionFound, channel.Writer);

        context.OnProgress.ShouldBeSameAs(progress);
        context.OnSolutionFound.ShouldBeSameAs(solutionFound);
        context.OnQueenPlaced.ShouldBeSameAs(channel.Writer);
        context.Cancellation.ShouldBe(cts.Token);
    }

    [Fact]
    public void SimulationContext_RecordEquality_ComparesByValue()
    {
        var a = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);
        var b = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void SimulationContext_With_ProducesModifiedCopy()
    {
        var original = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);
        var modified = original with { BoardSize = 12 };

        modified.BoardSize.ShouldBe(12);
        original.BoardSize.ShouldBe(8);
        (modified == original).ShouldBeFalse();
    }

    // ── ProgressInfo ──────────────────────────────────────────────────────────

    [Fact]
    public void ProgressInfo_StoresPercent_AndComparesByValue()
    {
        var info = new ProgressInfo(42.5);

        info.Percent.ShouldBe(42.5);
        info.ShouldBe(new ProgressInfo(42.5));
        info.ShouldNotBe(new ProgressInfo(43.0));
    }

    // ── SolutionFoundInfo ─────────────────────────────────────────────────────

    [Fact]
    public void SolutionFoundInfo_StoresPayload()
    {
        int[] board = [0, 2, 4, 1, 3];
        var info = new SolutionFoundInfo(board, 5, (UInt128)123);

        info.BoardSize.ShouldBe(5);
        info.PackedCanonical.ShouldBe((UInt128)123);
        info.Solution.Span.ToArray().ShouldBe(board);
    }

    // ── QueenPlacedInfo ───────────────────────────────────────────────────────

    [Fact]
    public void QueenPlacedInfo_StoresPayload()
    {
        int[] prefix = [0, 2];
        var info = new QueenPlacedInfo(prefix, 8, (UInt128)7);

        info.BoardSize.ShouldBe(8);
        info.PackedCanonical.ShouldBe((UInt128)7);
        info.Solution.Span.ToArray().ShouldBe(prefix);
    }
}
