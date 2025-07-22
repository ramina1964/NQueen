namespace NQueen.ViewModelTests.Tests.Main;

public class ProgressRelayTests
{
    [Fact]
    public async Task ProgressValue_IsUpdated_When_ProgressValueChangedMessage_Received()
    {
        // Arrange
        var dispatcher = new TestDispatcher();
        var solverMock = new Mock<ISolver>();
        var saveFileServiceMock = new Mock<ISaveFileDialogService>();
        var viewModel = new MainViewModel(solverMock.Object, dispatcher, saveFileServiceMock.Object);

        double testProgress = 0.42;

        // Act: Simulate the message as if sent by the backend event handler
        WeakReferenceMessenger.Default
            .Send(new ProgressValueChangedMessage(testProgress));

        // Allow async message dispatching to complete
        await Task.Delay(50);

        // Assert
        viewModel.ProgressValue.Should().BeApproximately(testProgress, 0.0001,
            "ProgressValue should be updated when ProgressValueChangedMessage is received");
    }

    [Fact]
    public async Task ProgressValue_IsUpdated_EndToEnd_When_BackendRaisesEvent()
    {
        WeakReferenceMessenger.Default.Reset();

        var dispatcher = new TestDispatcher();
        var saveFileServiceMock = new Mock<ISaveFileDialogService>();
        var solutionManagerMock = new Mock<ISolutionManager>();
        var orchestrator = new SimulationOrchestrator(solutionManagerMock.Object, 8);

        var viewModel = new MainViewModel(orchestrator, dispatcher, saveFileServiceMock.Object);

        double testProgress = 0.66;
        var token = Guid.NewGuid();

        var tokenField = typeof(MainViewModel)
            .GetField("_currentSimulationToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tokenField?.SetValue(viewModel, token);

        typeof(MainViewModel)
            .GetMethod("SubscribeToSimulationEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(viewModel, null);

        var eventField = typeof(SimulationOrchestrator)
            .GetField("ProgressValueChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var eventDelegate = eventField?.GetValue(orchestrator) as MulticastDelegate;

        if (eventDelegate is not null)
            eventDelegate.DynamicInvoke(orchestrator, new ProgressValueChangedWithTokenEventArgs(testProgress, token));
        else
            throw new InvalidOperationException("ProgressValueChanged event delegate not found.");

        // Wait for the property to update
        await TestHelpers.WaitForConditionAsync(() => viewModel.ProgressValue == testProgress, TimeSpan.FromSeconds(2));

        // Assert
        viewModel.ProgressValue.Should().BeApproximately(testProgress, 0.0001,
            "ProgressValue should be updated when backend raises ProgressValueChanged event end-to-end");

        // Clean up messenger state after test
        WeakReferenceMessenger.Default.UnregisterAll(viewModel);
    }
}
