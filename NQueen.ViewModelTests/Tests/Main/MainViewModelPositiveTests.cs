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
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mockSolver = TestHelpers.CreateMockSolver(
        [
            new Solution([1, 3, 0, 2], mockFormatter, null)
        ]);

        var simContext = new SimulationContext(
            boardSize, solutionMode, displayMode);

        var mainVm = TestHelpers.CreateMainViewModelWithMock(mockSolver.Object, simContext);

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
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode, null, mockFormatter);

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
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode, null, mockFormatter);

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
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        var mainVm = TestHelpers.CreateMainViewModel(
            boardSize, solutionMode, displayMode, null, mockFormatter);

        // Act
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        AssertionHelpers.AssertChessboardState(mainVm, boardSize);
    }

    [Fact]
    public async Task MainViewModel_ShouldUpdateSolutionsAfterSimulation()
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
        List<Solution> solutions = [new([1, 3, 0, 2], mockFormatter, null)];
        var mockSolver = TestHelpers.CreateMockSolver(solutions);

        var mainViewModel = new MainViewModel(
            mockSolver.Object,
            new TestDispatcher(),
            new MockSaveFileDialogService(),
            mockFormatter
        );

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
        // Arrange
        var mockSolver = new Mock<ISolver>();
        var mockFormatter = new Mock<ISolutionFormatter>().Object;

        mockSolver
            .Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .Returns(async () =>
            {
                if (solutionMode == SolutionMode.Single)
                    await Task.Delay(10);
                return new SimulationResults([new Solution([1, 3, 0, 2], mockFormatter, null)], 1.0);
            });

        var mainVm = new MainViewModel(
            mockSolver.Object,
            new TestDispatcher(),
            new MockSaveFileDialogService(),
            mockFormatter
        )
        {
            SolutionMode = solutionMode
        };

        // Act
        mainVm.SimulateCommand.Execute(null);

        // Assert initial state of IsSingleRunning
        mainVm.IsSingleRunning.Should().Be(expectedIndeterminate);

        // Wait for simulation to complete
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert final state of IsSingleRunning
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
    [InlineData(4, SolutionMode.Unique, DisplayMode.Visualize, 1)]
    [InlineData(4, SolutionMode.All, DisplayMode.Visualize, 2)]
    public async Task ProgressBar_ShouldUpdate_ForUniqueAndAllModes(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode,
        int expectedSolutionCount)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;

        var solutions = solutionMode switch
        {
            SolutionMode.All =>
            [
                new Solution([1, 3, 0, 2], mockFormatter, null),
                new Solution([2, 0, 3, 1], mockFormatter, null)
            ],
            _ => new[]
            {
                new Solution([1, 3, 0, 2], mockFormatter, null)
            }
        };

        var mockSolver = new Mock<ISolver>();

        mockSolver
            .Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(solutions, ElapsedTimeInSec: 1.0));

        mockSolver
            .SetupAdd(s => s.ProgressValueChanged += It.IsAny<EventHandler<ProgressUpdateEventArgs>>())
            .Callback<EventHandler<ProgressUpdateEventArgs>>(handler =>
            {
                for (var progress = 0.1; progress <= 1.0; progress += 0.1)
                {
                    handler?.Invoke(mockSolver.Object,
                        new ProgressUpdateEventArgs(progress * 100, Guid.NewGuid()));
                    Task.Delay(10).Wait();
                }
            });

        var simContext = new SimulationContext(
            boardSize, solutionMode, displayMode);

        var mainVm = TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object, simContext, simulationResults: null, mockFormatter);

        double? progressDuringSimulation = null;
        mainVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(mainVm.ProgressValue))
                progressDuringSimulation = mainVm.ProgressValue;
        };

        // Act
        mainVm.SimulateCommand.Execute(null);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Wait until at least one progress update observed (robust across modes)
        await TestHelpers.WaitForConditionAsync(
            () => progressDuringSimulation.HasValue,
            TimeSpan.FromSeconds(10));

        // Assert
        progressDuringSimulation.Should().NotBeNull("ProgressValue should update during simulation.");
        progressDuringSimulation.Should().BeGreaterThan(0, "Progress should advance above 0.");
        mainVm.ProgressValue.Should().BeInRange(0, 1);
        mainVm.ProgressVisibility.Should().Be(Visibility.Hidden);
        mainVm.ObservableSolutions.Count.Should().Be(expectedSolutionCount);
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
