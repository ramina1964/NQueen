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

        var mainVm = TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object, boardSize, solutionMode, displayMode, null, mockFormatter);

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

    // Todo: Fix for CS7036: Add the required 'solutionFormatter' argument when constructing MainViewModel
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
            .Setup(s => s.GetResultsForBoardAsync(It.IsAny<int>(), It.IsAny<SolutionMode>(), It.IsAny<DisplayMode>()))
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
    [InlineData(4, DisplayMode.Visualize)]
    public async Task ProgressBar_ShouldUpdate_ForUniqueMode(
        int boardSize, DisplayMode displayMode)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;

        // Create a mock solver for Unique mode
        var mockSolver = new Mock<ISolver>();

        // Configure the mock solver to return a valid SimulationResults object
        mockSolver
            .Setup(s => s.GetResultsForBoardAsync(boardSize, SolutionMode.Unique, displayMode))
            .ReturnsAsync(new SimulationResults(
                [
                    new Solution([1, 3, 0, 2], mockFormatter, null)
                ],
                ElapsedTimeInSec: 1.0
            ));

        // Simulate progress updates
        mockSolver
            .SetupAdd(s => s.ProgressValueChanged += It.IsAny<EventHandler<ProgressChangedWithTokenEventArgs>>())
            .Callback<EventHandler<ProgressChangedWithTokenEventArgs>>(handler =>
            {
                for (var progress = 0.1; progress <= 1.0; progress += 0.1)
                {
                    Console.WriteLine($"Triggering ProgressValueChanged with progress: {progress * 100}");
                    handler?.Invoke(mockSolver.Object, new ProgressChangedWithTokenEventArgs(progress * 100, Guid.NewGuid()));
                    Task.Delay(50).Wait();
                }
            });

        var mainVm = TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object,
            boardSize,
            SolutionMode.Unique,
            displayMode,
            null,
            mockFormatter
        );

        double? progressDuringSimulation = null;
        mainVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(mainVm.ProgressValue))
            {
                Console.WriteLine($"ProgressValue updated to: {mainVm.ProgressValue}");
                progressDuringSimulation = mainVm.ProgressValue;
            }
        };

        // Act
        Console.WriteLine("Starting simulation...");
        mainVm.SimulateCommand.Execute(null);

        // Wait for the simulation to complete
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Wait for the progress value to be updated
        await TestHelpers.WaitForConditionAsync(() =>
        {
            Console.WriteLine($"Checking progressDuringSimulation: {progressDuringSimulation}");
            return progressDuringSimulation.HasValue;
        }, TimeSpan.FromSeconds(20));

        // Assert
        Console.WriteLine("Asserting progressDuringSimulation...");
        progressDuringSimulation.Should().NotBeNull("ProgressValue should be updated during the simulation.");
        progressDuringSimulation.Should().BeGreaterThan(0, "ProgressValue should be greater than 0 during the simulation.");

        // Optionally, check final state
        mainVm.ProgressValue.Should().BeInRange(0, 1, "ProgressValue should be between 0 and 1 after the simulation.");
        mainVm.ProgressVisibility.Should().Be(Visibility.Hidden, "ProgressVisibility should be hidden after the simulation.");
    }

    [Theory]
    [InlineData(4, DisplayMode.Visualize)]
    public async Task ProgressBar_ShouldUpdate_ForAllMode(
        int boardSize, DisplayMode displayMode)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;
                
        // Create a mock solver for All mode
        var mockSolver = new Mock<ISolver>();
        mockSolver
            .Setup(s => s.GetResultsForBoardAsync(It.IsAny<int>(), SolutionMode.All, displayMode))
            .ReturnsAsync(new SimulationResults(
                [
                    new Solution([1, 3, 0, 2], mockFormatter, null),
                    new Solution([2, 0, 3, 1], mockFormatter, null)
                ],
                ElapsedTimeInSec: 1.0
            ));

        // Simulate progress updates
        mockSolver
            .SetupAdd(s => s.ProgressValueChanged += It.IsAny<EventHandler<ProgressChangedWithTokenEventArgs>>())
            .Callback<EventHandler<ProgressChangedWithTokenEventArgs>>(handler =>
            {
                for (var progress = 0.1; progress <= 1.0; progress += 0.1)
                {
                    handler?.Invoke(mockSolver.Object, new ProgressChangedWithTokenEventArgs(progress * 100, Guid.NewGuid()));
                    Task.Delay(50).Wait();
                }
            });

        var mainVm = TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object,
            boardSize,
            SolutionMode.All,
            displayMode,
            null,
            mockFormatter
        );

        double? progressDuringSimulation = null;
        mainVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(mainVm.ProgressValue))
                progressDuringSimulation = mainVm.ProgressValue;
        };

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
