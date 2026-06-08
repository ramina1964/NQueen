namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelPositiveTests
{
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

    [Fact]
    public async Task ProgressBar_ShouldUpdate_ForSingleMode()
    {
        // Arrange: real solver — Single mode does not use the mock progress-event path
        var mainVm = TestHelpers.CreateMainViewModel(4, SolutionMode.Single, DisplayMode.Visualize);

        // Act
        mainVm.SimulateCommand.Execute(null);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        // Assert
        mainVm.ProgressValue.Should().BeInRange(0, 1);
    }

    [Theory]
    [InlineData(4, SolutionMode.Unique, DisplayMode.Visualize, 1)]
    [InlineData(4, SolutionMode.All, DisplayMode.Visualize, 2)]
    public async Task ProgressBar_ShouldUpdate(
        int boardSize,
        SolutionMode solutionMode,
        DisplayMode displayMode,
        int expectedSolutionCount)
    {
        // Arrange
        var mockFormatter = new Mock<ISolutionFormatter>().Object;

        var solutions = solutionMode == SolutionMode.All
            ? new[] { new Solution([1, 3, 0, 2], mockFormatter, null), new Solution([2, 0, 3, 1], mockFormatter, null) }
            : new[] { new Solution([1, 3, 0, 2], mockFormatter, null) };

        var mockSolver = new Mock<ISolver>();

        mockSolver
            .Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .Returns<SimulationContext>(async ctx =>
            {
                // Drive progress through the per-call IProgress<ProgressInfo> sink the VM supplies,
                // exactly as the real solver does (replaces the former ProgressValueChanged event).
                for (var progress = 0.1; progress <= 1.0; progress += 0.1)
                {
                    ctx.OnProgress?.Report(new ProgressInfo(progress * 100));
                    await Task.Delay(10);
                }
                return new SimulationResults(solutions, ElapsedTimeInSec: 1.0);
            });

        var mainVm = TestHelpers.CreateMainViewModelWithMock(
            mockSolver.Object,
            new SimulationContext(boardSize, solutionMode, displayMode),
            simulationResults: null,
            mockFormatter);

        double? progressDuringSimulation = null;
        mainVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(mainVm.ProgressValue))
                progressDuringSimulation = mainVm.ProgressValue;
        };

        // Act
        mainVm.SimulateCommand.Execute(null);
        await TestHelpers.WaitForSimulationCompletionAsync(mainVm);

        await TestHelpers.WaitForConditionAsync(
            () => progressDuringSimulation.HasValue,
            TimeSpan.FromSeconds(10));

        // Assert
        progressDuringSimulation.Should().NotBeNull("ProgressValue should update during simulation.");
        progressDuringSimulation.Should().BeGreaterThan(0, "Progress should advance above 0.");
        mainVm.ProgressValue.Should().BeInRange(0, 1);
        // After a completed simulation the progress bar is Collapsed (not Hidden): Collapsed frees
        // the layout row so the Simulation panel shrinks to its content and no longer reserves
        // space that would clip the Solver Settings card below it.
        mainVm.ProgressVisibility.Should().Be(Visibility.Collapsed);
        mainVm.ObservableSolutions.Count.Should().Be(expectedSolutionCount);
    }


    [Fact]
    public void SaveSimulationResultsCommand_ShouldWriteContentViaService()
    {
        // Arrange
        var mockSaveService = new MockSaveFileDialogService();
        var solutionFormatter = new SolutionFormatter();
        var queenPositions = new int[] { 1, 3, 0, 2 };
        var solution = new Solution(queenPositions, solutionFormatter, 1);

        var mainVm = new MainViewModel(
            TestHelpers.CreateMockSolver([solution]).Object,
            new TestDispatcher(),
            mockSaveService,
            solutionFormatter)
        {
            BoardSizeText = "4",
            SolutionMode = SolutionMode.Single,
            NoOfSolutions = "1",
            IsIdle = true,
        };

        // Populate ObservableSolutions and mark output as ready — same state
        // the VM is in after a completed simulation before the user clicks Save.
        mainVm.ObservableSolutions.Add(solution);
        mainVm.IsOutputReady = true;

        // Act
        mainVm.SaveCommand.Execute(null);

        // Assert
        mockSaveService.WasCalled.Should().BeTrue("the save service should be invoked");
        mockSaveService.SavedContent.Should().NotBeNullOrEmpty("saved content must not be empty");
        mockSaveService.SavedContent.Should().Contain("4", "content should include the board size");
        mockSaveService.SavedContent.Should().Contain("Single", "content should include the solution mode");
    }
}

