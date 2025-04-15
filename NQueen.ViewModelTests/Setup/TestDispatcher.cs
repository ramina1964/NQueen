namespace NQueen.ViewModelTests.Setup;

public class TestDispatcher : IDispatcher
{
    public void Invoke(Action action) => action();

    public void BeginInvoke(Action action, DispatcherPriority pr = DispatcherPriority.Normal) =>
        action();
}
