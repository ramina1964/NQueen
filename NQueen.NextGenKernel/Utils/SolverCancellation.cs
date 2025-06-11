namespace NQueen.NextGenKernel.Utils;

public class SolverCancellation : IDisposable
{
    public bool IsCanceled => _cts.IsCancellationRequested;

    public CancellationToken Token => _cts.Token;

    public void Cancel() => _cts.Cancel();

    public void Reset() => _cts = new CancellationTokenSource();

    public void Dispose() => _cts.Dispose();

    private CancellationTokenSource _cts = new();
}
