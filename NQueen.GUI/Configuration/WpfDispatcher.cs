namespace NQueen.GUI.Configuration;

public class WpfDispatcher : IDispatcher
{
    public void Invoke(Action action) =>
        Application.Current?.Dispatcher?.Invoke(action);

    public void BeginInvoke(Action ac, DispatcherPriority pr = DispatcherPriority.Normal)
        => Application.Current?.Dispatcher?.BeginInvoke(pr, ac);
}
