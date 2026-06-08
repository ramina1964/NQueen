namespace NQueen.Domain.Context;

/// <summary>Queen-placement (partial-prefix) notification payload pushed through the
/// <see cref="SimulationContext.OnQueenPlaced"/> conflating channel. <see cref="Solution"/> must
/// wrap a copied buffer because the channel drain is deferred to the UI <c>DispatcherTimer</c>.</summary>
public readonly record struct QueenPlacedInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical);
