namespace NQueen.ViewModelTests.Setup;

public static class TestHelpers
{
    public static MainViewModel CreateMainViewModel(
        int boardSize = 8,
        SolutionMode solutionMode = SolutionMode.Single,
        DisplayMode displayMode = DisplayMode.Hide,
        SimulationResults? simulationResults = null)
    {
        return new MainViewModel(new BackTrackingSolver(new SolutionManager()))
        {
            BoardSizeText = boardSize.ToString(),
            SolutionMode = solutionMode,
            DisplayMode = displayMode,
            SimulationResults = simulationResults ?? new SimulationResults([])
        };
    }
}
