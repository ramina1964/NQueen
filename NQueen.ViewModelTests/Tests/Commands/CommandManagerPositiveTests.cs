namespace NQueen.ViewModelTests.Tests.Commands;

[Trait("Category", "Commands")]
public class CommandManagerPositiveTests
{
    [Theory]
    [InlineData("1", SolutionMode.Single, DisplayMode.Hide)]
    [InlineData("4", SolutionMode.Unique, DisplayMode.Visualize)]
    [InlineData("4", SolutionMode.All,    DisplayMode.Hide)]
    [InlineData("8", SolutionMode.Single, DisplayMode.Visualize)]
    public async Task SimulateCommand_ShouldUpdateSimulationResults(
        string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModelWithBoardSizeText(
            boardSizeText, solutionMode, displayMode, null, true, int.MaxValue);

        var tcs = new TaskCompletionSource<bool>();
        mainVm.SimulationCompleted += (s, e) => tcs.SetResult(true);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await tcs.Task;

        // Assert
        mainVm.SimulationResults.Solutions.ShouldNotBeEmpty();
        mainVm.IsSimulating.ShouldBeFalse();
    }

    [Fact]
    public void CancelCommand_ShouldStopSimulation()
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            8, SolutionMode.Single, DisplayMode.Visualize);

        mainVm.IsSimulating = true;

        // Act
        mainVm.CancelCommand.Execute(null);

        // Assert
        mainVm.IsSimulating.ShouldBeFalse();
    }

    [Theory]
    [InlineData("4", SolutionMode.Single, DisplayMode.Hide)]
    public void SaveCommand_ShouldProcessSimulationResults(
        string boardSizeText, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModelWithBoardSizeText(
            boardSizeText, solutionMode, displayMode);

        var solutionFormatter = new SolutionFormatter();

        var queenPositions = new int[] { 1, 3, 0, 2 };
        var solution = new Solution(queenPositions, solutionFormatter, 1);
        mainVm.SimulationResults = new SimulationResults([solution], 0.0);
        mainVm.NoOfSolutions = "1";
        mainVm.IsIdle = true;

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
            mainVm.IsIdle.ShouldBeTrue();
            }
        }
