namespace NQueen.ViewModelTests.Tests.Main;

public class ProgressRelayTests
{
    [Fact]
    public async Task ProgressValue_IsUpdated_When_ProgressValueChangedMessage_Received()
    {
        // Arrange
        var viewModel = TestHelpers.CreateMainViewModel();

        int testProgress = 42;

        // Act: Simulate the message as if sent by the backend event handler
        WeakReferenceMessenger.Default
            .Send(new ProgressValueChangedMessage(testProgress));

        // Allow async message dispatching to complete
        await Task.Delay(50);

        // Assert
        viewModel.ProgressValue.Should().Be(testProgress,
            "ProgressValue should be updated when ProgressValueChangedMessage is received");
    }

    [Fact]
    public async Task ProgressValue_IsUpdated_EndToEnd_When_BackendRaisesEvent()
    {
        WeakReferenceMessenger.Default.Reset();

        // Use DI to get orchestrator and viewmodel
        var serviceProvider = TestHelpers.CreateServiceProvider();

        // Get the orchestrator as ISolver and cast to SimulationOrchestrator if needed
        var orchestrator = serviceProvider.GetRequiredService<ISolverBackEnd>() as SimulationOrchestrator;
        var viewModel = serviceProvider.GetRequiredService<MainViewModel>();

        int testProgress = 42;
        var token = Guid.NewGuid();

        // Set the token on the viewmodel if needed
        var tokenField = typeof(MainViewModel)
            .GetField("_currentSimulationToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tokenField?.SetValue(viewModel, token);

        typeof(MainViewModel)
            .GetMethod("SubscribeToSimulationEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(viewModel, null);

        // Raise the event via orchestrator (if orchestrator is not null)
        orchestrator?.RaiseProgressValueChangedForTest(testProgress, token);

        // Wait for the property to update
        await TestHelpers.WaitForConditionAsync(() =>
            viewModel.ProgressValue == testProgress, TimeSpan.FromSeconds(2));

        // Assert
        viewModel.ProgressValue.Should().BeApproximately(testProgress, 0.0001,
            "ProgressValue should be updated when backend raises ProgressValueChanged event end-to-end");

        // Clean up messenger state after test
        WeakReferenceMessenger.Default.UnregisterAll(viewModel);
    }
}
