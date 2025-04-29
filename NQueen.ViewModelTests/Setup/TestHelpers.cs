namespace NQueen.ViewModelTests.Setup;

public static class TestHelpers
{
    // Use the test-specific service registration
    public static IServiceProvider CreateServiceProvider() =>
        TestServiceCollectionExtensions.InitializeForTests();

    public static MainViewModel CreateMainViewModel(
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null)
    {
        var serviceProvider = CreateServiceProvider();
        var mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();

        // Configure the MainViewModel instance
        mainViewModel.BoardSizeText = boardSize.ToString();
        mainViewModel.SolutionMode = solutionMode;
        mainViewModel.DisplayMode = displayMode;
        mainViewModel.SimulationResults = simulationResults ?? new SimulationResults([]);

        return mainViewModel;
    }
}
