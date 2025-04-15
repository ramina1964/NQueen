namespace NQueen.GUI.Interfaces;

public interface IDispatcher
{
    void Invoke(Action action);

    void BeginInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal);
}
