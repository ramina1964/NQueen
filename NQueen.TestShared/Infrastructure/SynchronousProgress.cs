namespace NQueen.TestShared.Infrastructure;

/// <summary>
/// An <see cref="IProgress{T}"/> whose <see cref="Report"/> runs the handler <b>synchronously</b>
/// on the calling thread, unlike <see cref="System.Progress{T}"/> which posts asynchronously to a
/// captured <see cref="System.Threading.SynchronizationContext"/>.
/// </summary>
/// <remarks>
/// Tests use this to observe solver notifications deterministically: the solver reports from a
/// reused buffer on its own thread, so a synchronous sink lets a test count callbacks (and toggle
/// in-flight cancellation) without racing an asynchronous <see cref="System.Progress{T}"/> post.
/// </remarks>
public sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
{
    private readonly Action<T> _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public void Report(T value) => _handler(value);
}
