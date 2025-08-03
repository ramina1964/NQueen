namespace NQueen.NextGenKernel.Utils;

// Todo: Find out if and how this class can be used, otherwise remove it.
public class SolverCancellation : IDisposable
{
    public bool IsCanceled => _cts.IsCancellationRequested;

    public CancellationToken Token => _cts.Token;

    public void Cancel() => _cts.Cancel();

    public void Reset() => _cts = new CancellationTokenSource();

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private CancellationTokenSource _cts = new();
}
