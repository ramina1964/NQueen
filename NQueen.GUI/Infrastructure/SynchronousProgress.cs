namespace NQueen.GUI.Infrastructure;

/// <summary>
/// An <see cref="IProgress{T}"/> whose <see cref="Report"/> runs the handler <b>synchronously</b>
/// on the calling thread, unlike <see cref="System.Progress{T}"/> which posts asynchronously to a
/// captured <see cref="System.Threading.SynchronizationContext"/>.
/// </summary>
/// <remarks>
/// Required for the solution stream: the solver reports from a reused buffer that is overwritten
/// as the depth-first search continues, so the handler must copy the payload (e.g. via
/// <c>Memory&lt;int&gt;.ToArray()</c>) <i>before</i> control returns to the solver. The handler is
/// responsible for any UI-thread marshalling it needs (the view-model already wraps its
/// observable-collection mutations in an <c>IDispatcher.Invoke</c>).
/// </remarks>
public sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    private readonly Action<T> _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public void Report(T value) => _handler(value);
}
