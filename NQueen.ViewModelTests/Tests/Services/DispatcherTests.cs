namespace NQueen.ViewModelTests.Tests.Services;

public class DispatcherTests
{
    [Fact]
    public void Dispatcher_ShouldInvokeAction()
    {
        // Arrange
        var dispatcher = new Mock<IDispatcher>();
        var actionInvoked = false;

        dispatcher.Setup(d => d.Invoke(It.IsAny<Action>()))
            .Callback<Action>(action => action.Invoke());

        // Act
        dispatcher.Object.Invoke(() => actionInvoked = true);

        // Assert
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public Task Dispatcher_ShouldBeginInvokeAction()
    {
        // Arrange
        var dispatcher = new Mock<IDispatcher>();
        var actionInvoked = false;

        dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>(), It.IsAny<DispatcherPriority>()))
            .Callback<Action, DispatcherPriority>((action, _) => action.Invoke());

        // Act
        dispatcher.Object.BeginInvoke(() => actionInvoked = true, DispatcherPriority.Normal);

        // Assert
        actionInvoked.Should().BeTrue();

        return Task.CompletedTask;
    }
}
