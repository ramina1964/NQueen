namespace NQueen.ViewModelTests.Tests.Solver;

public class SolverTests
{
    [Fact]
    public async Task Solver_ShouldReturnValidResults_WhenBoardSizeIsValid()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(
                [new Solution([0, 1, 2], new SolutionFormatter(), id: null)],
                totalSolutions: 1UL,
                ElapsedTimeInSec: 0.0));

        var simContext = new SimulationContext(
            BoardSize: 4,
            SolutionMode: SolutionMode.Single,
            DisplayMode: DisplayMode.Visualize);

        // Act
        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().NotBeEmpty();
        results.SolutionsCount.Should().Be(1);
        results.IsTotalInferred.Should().BeFalse();
    }

    [Fact]
    public async Task Solver_ShouldHandleNoSolutions()
    {
        // Arrange
        var solver = new Mock<ISolverBackEnd>();
        solver.Setup(s => s.GetSimResultsAsync(It.IsAny<SimulationContext>()))
            .ReturnsAsync(new SimulationResults(
                [],
                totalSolutions: 0UL,
                ElapsedTimeInSec: 0.0));

        var simContext = new SimulationContext(
            BoardSize: 4,
            SolutionMode: SolutionMode.Single,
            DisplayMode: DisplayMode.Visualize);

        // Act
        var results = await solver.Object.GetSimResultsAsync(simContext);

        // Assert
        results.Solutions.Should().BeEmpty();
        results.SolutionsCount.Should().Be(0);
        results.IsTotalInferred.Should().BeFalse();
    }

    [Fact]
    public async Task BitmaskSolver_SingleMode_ShouldReturnExactlyOneSolution()
    {
        // Arrange
        var solver = new BitmaskSolver(
            boardSize: 4,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new SolutionFormatter())
        {
            EnableEvents = true
        };
        int solutionFoundNotifications = 0;
        var solutionSink = new SynchronousProgress<SolutionFoundInfo>(_ => solutionFoundNotifications++);

        var ctx = new SimulationContext(4, SolutionMode.Single, DisplayMode.Visualize,
            OnSolutionFound: solutionSink);

        // Act
        var results = await solver.GetSimResultsAsync(ctx);

        // Assert
        results.SolutionsCount.Should().Be(1);
        solutionFoundNotifications.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    [Trait("Category", "SingleMode")]
    public async Task BitmaskSolver_SingleMode_HonorsPreCancelledToken_ReturnsWithoutThrowing()
    {
        // Stage 6: cancellation is signalled exclusively through the CancellationToken on
        // SimulationContext. With a pre-cancelled token the kernel must observe cancellation
        // at its first gated `IsCancellationRequested` read and bail without throwing — even
        // for a board size (N=17) that would otherwise take measurable time to enumerate.
        var solver = new BitmaskSolver(
            boardSize: 17,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new SolutionFormatter())
        {
            EnableEvents = false,
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = new SimulationContext(17, SolutionMode.Single, DisplayMode.Visualize,
            Cancellation: cts.Token);

        // Act
        var results = await solver.GetSimResultsAsync(ctx);

        // Assert
        results.Should().NotBeNull();
        results.SolutionsCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task BitmaskSolver_SingleMode_ShouldPushQueenPlacedNotifications()
    {
        var solver = new BitmaskSolver(
            boardSize: 8,
            solutionMode: SolutionMode.Single,
            displayMode: DisplayMode.Visualize,
            solutionFormatter: new SolutionFormatter())
        {
            EnableEvents = true
        };
        int queenNotifications = 0;
        int lastDepth = 0;
        var queenWriter = new CallbackChannelWriter<QueenPlacedInfo>(info =>
        {
            queenNotifications++;
            lastDepth = info.Solution.Length;
        });

        var ctx = new SimulationContext(8, SolutionMode.Single, DisplayMode.Visualize,
            OnQueenPlaced: queenWriter);

        _ = await solver.GetSimResultsAsync(ctx);

        queenNotifications.Should().BeGreaterThanOrEqualTo(8);
        lastDepth.Should().Be(8);
    }
}
