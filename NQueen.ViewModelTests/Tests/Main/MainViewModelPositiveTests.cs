namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelPositiveTests : IDisposable
{
    public MainViewModelPositiveTests() =>
        _serviceProvider = TestHelpers.CreateServiceProvider();

    [Theory]
    [InlineData(4, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Chessboard_ShouldUpdateQueenPlacements(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        AssertionHelpers.AssertChessboardState(mainVm, boardSize);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Solutions_ShouldUpdateDuringSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        await TestHelpers.WaitForConditionAsync(() =>
            mainVm.ObservableSolutions.Any(), TimeSpan.FromSeconds(5));

        // Assert
        AssertionHelpers.AssertSolutionsState(mainVm);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public void Cancel_ShouldStopSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        mainVm.IsSimulating = true;

        // Act
        mainVm.CancelCommand.Execute(null);

        // Assert
        mainVm.IsSimulating.Should().BeFalse(TestConst.SimulationNotStoppedError);
    }

    [Theory]
    [InlineData(8, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task Visualization_ShouldUpdateDuringSimulation(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        AssertionHelpers.AssertChessboardState(mainVm, boardSize);
    }

    [Theory]
    [MemberData(nameof(NQueenTestSets.SmallValueCases), MemberType = typeof(NQueenTestSets))]
    public void Save_ShouldProcessSimulationResults(
        int boardSize, SolutionMode solutionMode)
    {
        // Arrange
        var mockSaveFileDialogService = new MockSaveFileDialogService();
        var mainVm = TestHelpers.CreateMainViewModelWithSimulationResults(
            boardSize, solutionMode, mockSaveFileDialogService);

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
        mockSaveFileDialogService.WasCalled.Should().BeTrue(TestConst.SaveDialogNotShownError);

        var savedContent = mockSaveFileDialogService.SavedContent;
        savedContent.Should().NotBeNullOrEmpty(TestConst.ContentNotSavedError);

        AssertSavedContentProperties(savedContent!, boardSize,
            solutionMode, mainVm.SimulationResults);
    }

    [Fact]
    public async Task MainViewModel_ShouldUpdateSolutionsAfterSimulation()
    {
        // Arrange
        List<Solution> solutions = [new([1, 3, 0, 2])];
        var mockSolver = TestHelpers.CreateMockSolver(solutions);

        var mainViewModel = new MainViewModel(
            mockSolver.Object,
            new TestDispatcher(),
            new MockSaveFileDialogService());

        // Act
        await mainViewModel.SimulateCommand.ExecuteAsync(null);

        // Assert
        AssertionHelpers.AssertSolutionsState(mainViewModel);
    }

    [Theory]
    [InlineData(SolutionMode.Single, true)]
    [InlineData(SolutionMode.Unique, false)]
    [InlineData(SolutionMode.All, false)]
    public async Task IsSingleRunning_ShouldReflectSolutionMode(
        SolutionMode solutionMode, bool expectedIndeterminate)
    {
        var mockSolver = new Mock<ISolver>();
        mockSolver
            .Setup(s => s.GetResultsForBoardAsync(It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
            .Returns(async () =>
            {
                if (solutionMode == SolutionMode.Single)
                    await Task.Delay(10);
                return new SimulationResults([new Solution([1, 3, 0, 2])]);
            });

        var mainVm = new MainViewModel(
            mockSolver.Object,
            new TestDispatcher(),
            new MockSaveFileDialogService()
        )
        {
            SolutionMode = solutionMode
        };

        mainVm.SimulateCommand.Execute(null);
        if (solutionMode == SolutionMode.Single)
            await Task.Delay(2);

        mainVm.IsSingleRunning.Should().Be(expectedIndeterminate);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);
        mainVm.IsSingleRunning.Should().BeFalse();
    }

    [Theory]
    [InlineData(4, SolutionMode.Single, DisplayMode.Visualize)]
    public async Task ProgressBar_ShouldUpdate_ForSingleMode(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(boardSize, solutionMode, displayMode);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        mainVm.ProgressValue.Should().BeInRange(0, 1);
    }

    [Theory]
    [InlineData(4, SolutionMode.Unique, DisplayMode.Visualize)]
    [InlineData(4, SolutionMode.All, DisplayMode.Visualize)]
    public async Task ProgressBar_ShouldUpdate_ForUniqueAndAllModes(
        int boardSize, SolutionMode solutionMode, DisplayMode displayMode)
    {
        // Arrange
        var mainVm = TestHelpers.CreateMainViewModel(boardSize, solutionMode, displayMode);

        // Todo: Remove the else branch as it is not needed.
        double? progressDuringSimulation = null;
        if (mainVm is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(mainVm.ProgressValue))
                    progressDuringSimulation = mainVm.ProgressValue;
            };
        }
        else
        {
            throw new InvalidOperationException("MainViewModel does not implement INotifyPropertyChanged");
        }

        // Act
        mainVm.SimulateCommand.Execute(null);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        progressDuringSimulation.Should().NotBeNull();
        progressDuringSimulation.Should().BeGreaterThan(0);

        // Optionally, check final state
        mainVm.ProgressValue.Should().BeInRange(0, 1);
        mainVm.ProgressVisibility.Should().Be(Visibility.Hidden);
    }

    private static void AssertSavedContentProperties(
        string savedContent,
        int boardSize,
        SolutionMode solutionMode,
        SimulationResults simulationResults)
    {
        var lines = savedContent.Split(['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries);

        var dict = new Dictionary<string, string>();
        foreach (var line in lines)
        {
            var idx = line.IndexOf(':');
            if (idx > 0)
            {
                var key = line[..idx].Trim();
                var value = line[(idx + 1)..].Trim();
                dict[key] = value;
            }
        }

        dict.Should().ContainKey("Date && Time");
        dict["Date && Time"].Should().NotBeNullOrWhiteSpace();

        dict.Should().ContainKey("Board Size");
        dict["Board Size"].Should().Be(boardSize.ToString());

        dict.Should().ContainKey("SolutionMode");
        dict["SolutionMode"].Should().Be(solutionMode.ToString());

        dict.Should().ContainKey("Number of Solutions");
        dict["Number of Solutions"].Should()
            .Be(simulationResults.Solutions.Count().ToString());

        dict.Should().ContainKey("Max Number of Solutions Included");
        dict["Max Number of Solutions Included"].Should()
            .Be(SimulationSettings.MaxNoOfSolutionsInOutput.ToString());

        dict.Should().ContainKey("Elapsed Time");
        dict["Elapsed Time"].Should().Contain("seconds");

        dict.Should().ContainKey("Memory Usage");
        dict["Memory Usage"].Should().Contain("MB");
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    private readonly ServiceProvider _serviceProvider;
}
