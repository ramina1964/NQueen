namespace NQueen.TestShared.Infrastructure;

/// <summary>
/// A <see cref="ChannelWriter{T}"/> that invokes a callback <b>synchronously</b> on every
/// <see cref="TryWrite"/>. Lets a test observe the solver's QueenPlaced stream on the producing
/// thread — counting placements or toggling in-flight cancellation — without the conflating
/// drop-oldest semantics of the production UI channel.
/// </summary>
public sealed class CallbackChannelWriter<T>(Action<T> onWrite) : ChannelWriter<T>
{
    private readonly Action<T> _onWrite = onWrite ?? throw new ArgumentNullException(nameof(onWrite));

    public override bool TryWrite(T item)
    {
        _onWrite(item);
        return true;
    }

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<bool>(cancellationToken)
            : new ValueTask<bool>(true);
}
