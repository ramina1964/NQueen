namespace NQueen.ViewModelTests.Setup;

public static class TestHelpers
{
    // Use the test-specific service registration
    public static IServiceProvider CreateServiceProvider() =>
        ServiceCollectionExtensions.InitializeForTests();

    public static MainViewModel CreateMainViewModel(
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null)
    {
        return new MainViewModel(new BackTrackingSolver(new SolutionManager()), new TestDispatcher())
        {
            BoardSizeText = boardSize.ToString(),
            SolutionMode = solutionMode,
            DisplayMode = displayMode,
            SimulationResults = simulationResults ?? new SimulationResults([])
        };
    }
}
