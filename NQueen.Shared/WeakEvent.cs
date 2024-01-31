namespace NQueen.Shared;

public class WeakEvent<TEventArgs> where TEventArgs : EventArgs
{
    public void Add(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            RemoveDeadHandlers();
            _handlers.Add(new WeakReference<EventHandler<TEventArgs>>(handler));
        }
    }

    public void Remove(EventHandler<TEventArgs> handler)
    {
        lock (_handlers)
        {
            RemoveDeadHandlers();
            _handlers.RemoveAll(reference =>
            {
                if (reference.TryGetTarget(out var targetHandler))
                {
                    return targetHandler == handler;
                }
                return true;
            });
        }
    }

    public void Raise(object sender, TEventArgs e)
    {
        lock (_handlers)
        {
            RemoveDeadHandlers();
            foreach (var reference in _handlers)
            {
                if (reference.TryGetTarget(out var handler))
                {
                    handler(sender, e);
                }
            }
        }
    }

    private void RemoveDeadHandlers()
    {
        _handlers.RemoveAll(reference => !reference.TryGetTarget(out _));
    }

    private readonly List<WeakReference<EventHandler<TEventArgs>>> _handlers = [];
}
