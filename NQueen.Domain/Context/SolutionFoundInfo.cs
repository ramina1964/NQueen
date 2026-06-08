namespace NQueen.Domain.Context;

/// <summary>Solution-found notification payload pushed through the
/// <see cref="SimulationContext.OnSolutionFound"/> sink.</summary>
public readonly record struct SolutionFoundInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical);
