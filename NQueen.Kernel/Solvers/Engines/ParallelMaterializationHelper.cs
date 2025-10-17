namespace NQueen.Kernel.Solvers.Engines;

/// <summary>
/// Helper for parallel N-Queens solvers to handle materialization up to a cap, then fast local counting.
/// </summary>
internal static class ParallelMaterializationHelper
{
    // Handles materialization up to cap, then switches to local counting for All/Unique modes.
    // The callback is only invoked for the first 'cap' solutions; after that, only localCount is incremented.
    public static void HandleMaterialization<T>(
        ref int globalMaterialized,
        int cap,
        ref bool capReached,
        T solution,
        Action<T> onMaterialize,
        ref ulong localCount)
    {
        if (!capReached)
        {
            int mat = globalMaterialized < cap ? System.Threading.Interlocked.Increment(ref globalMaterialized) : cap + 1;
            if (mat <= cap)
            {
                onMaterialize(solution);
            }
            else
            {
                capReached = true;
                localCount++;
            }
        }
        else
        {
            // After cap: no atomic, no delegate, just count
            localCount++;
        }
    }
}
