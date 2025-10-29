namespace NQueen.ViewModelTests.Tests.Main;

public class ProgressRelayTests : IDisposable
{
    public ProgressRelayTests()
    {
        _sp = TestHelpers.CreateServiceProvider();
        _formatter = _sp.GetRequiredService<ISolutionFormatter>();
    }

    [Fact]
    public async Task Heartbeat_ShouldSyntheticAdvance_WhenNoRealProgress()
    {
        // Arrange: mock solver that performs a long-running task WITHOUT firing progress events.
        var mockSolver = new Mock<ISolver>();
        mockSolver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .Returns(async () =>
            {
                // Delay longer than heartbeat interval so synthetic tick would be needed.
                await Task.Delay(SimulationSettings.ProgressIntervalInMilliSec + 2000);
                // Return a trivial solution set so simulation can finish cleanly.
                return new SimulationResults(new[] { new Solution(new[] { 0 }, _formatter, null) }, 0.0);
            });

        var ctx = new SimulationContext(8, SolutionMode.Unique, DisplayMode.Hide);
        var vm = TestHelpers.CreateMainViewModelWithMock(mockSolver.Object, ctx, simulationResults: null, _formatter);

        // Act: start simulation (async) but do NOT await completion yet.
        vm.SimulateCommand.Execute(null);

        // Small delay to allow MainViewModel to initialize progress & heartbeat timer.
        await Task.Delay(150);
        vm.IsSimulating.Should().BeTrue("Simulation should have started.");
        vm.ProgressPercent.Should().Be(0, "Initial progress percent should be0 before heartbeat fires.");

        // Force internal state to appear silent past the heartbeat threshold.
        var lastUpdateField = vm.GetType().GetField("_lastProgressUpdateUtc", BindingFlags.NonPublic | BindingFlags.Instance);
        lastUpdateField.Should().NotBeNull();
        lastUpdateField!.SetValue(vm, DateTime.UtcNow - TimeSpan.FromMilliseconds(SimulationSettings.ProgressIntervalInMilliSec + 500));

        // Invoke private heartbeat tick method via reflection to simulate timer firing.
        var tickMethod = vm.GetType().GetMethod("ProgressHeartbeatTimer_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
        tickMethod.Should().NotBeNull();
        tickMethod!.Invoke(vm, new object?[] { null, EventArgs.Empty });

        // Assert: synthetic progress advanced above0 but below cap (95).
        vm.ProgressPercent.Should().BeGreaterThan(0, "Heartbeat should increment progress after silence interval.");
        vm.ProgressPercent.Should().BeLessThan(96, "Synthetic progress must stay below95 cap.");

        // Cleanup: allow solver task to finish.
        await TestHelpers.WaitForSimulationCompletionAsync(vm);
    }

    public void Dispose()
    {
        _sp.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly ServiceProvider _sp;
    private readonly ISolutionFormatter _formatter;
}
