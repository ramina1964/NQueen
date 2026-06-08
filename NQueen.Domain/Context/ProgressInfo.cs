namespace NQueen.Domain.Context;

/// <summary>Progress notification payload pushed through the
/// <see cref="SimulationContext.OnProgress"/> sink. Replaces the event-based
/// <c>ProgressUpdateEventArgs</c>; the per-run Guid correlation token is no longer
/// needed because a fresh sink is created per simulation.</summary>
public readonly record struct ProgressInfo(double Percent);
