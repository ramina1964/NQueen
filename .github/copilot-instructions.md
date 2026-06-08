# Copilot Instructions

## Project Guidelines

### Language & Framework
- C# 14, .NET 10, targeting `net10.0` across all projects.
- WPF for the GUI project (`NQueen.GUI`); console runner in `NQueen.Console`.

### Project Structure
| Project | Purpose |
|---|---|
| `NQueen.Domain` | Interfaces, models, enums, context records, settings, utilities — no solver logic |
| `NQueen.Kernel` | Solver implementations (`NQueen.Kernel.Solvers`) and DI extensions |
| `NQueen.Shared` | Cross-cutting helpers (parsing, numerics) |
| `NQueen.GUI` | WPF MVVM front-end |
| `NQueen.Console` | Console runner |
| `NQueen.UnitTests` | Kernel / domain unit tests |
| `NQueen.ViewModelTests` | ViewModel-level integration tests |
| `NQueen.TestShared` | Shared test infrastructure |
| `NQueen.Benchmarking` | BenchmarkDotNet benchmarks |

### Solver Conventions
- All solver classes live under `NQueen.Kernel.Solvers` (namespace and folder).
- `BitmaskSolver` is the single concrete solver; it is split across partial-class files:
  `BitmaskSolver.cs`, `BitmaskSolver.All.cs`, `BitmaskSolver.Single.cs`, `BitmaskSolver.Unique.cs`,
  `BitmaskSolver.CountUnique.cs`, `BitmaskSolver.Materialize.cs`.
- `BitboardNQueenSolver` is a `static` pure-count utility used internally by `BitmaskSolver`.
- Sink payload records (`ProgressInfo`, `SolutionFoundInfo`, `QueenPlacedInfo`) use `Memory<int>`
  (not `int[]`) for the board buffer and live in `NQueen.Domain.Context`.

### Interface Conventions
- `ISolverFrontEnd` — `DelayInMillisec` + `ProgressValue`; implemented by `BitmaskSolver`.
- `ISolverBackEnd` — `UseCountOnlyAllMode`, `UseCountOnlyUniqueMode`,
  `GetSimResultsAsync(SimulationContext)`; implemented by `BitmaskSolver`.
- Notifications and cancellation flow per-call via `SimulationContext`:
  `IProgress<ProgressInfo>` (progress), `IProgress<SolutionFoundInfo>` (solutions, synchronous
  via `SynchronousProgress<T>`), `ChannelWriter<QueenPlacedInfo>` (animation, conflating
  bounded channel with `DropOldest`), and a `CancellationToken`.
- `ISolver` combines both: `ISolverBackEnd + ISolverFrontEnd`.

### Coding Style
- Global usings are declared per-project in `Usings.cs`.
- Prefer `partial` classes to split large ViewModels (e.g., `MainViewModel.Events.cs`).
- `[MethodImpl(AggressiveInlining | AggressiveOptimization)]` on hot-path solver methods.
- No magic strings for property names — use `nameof(...)`.

### Changelog
- Update `CHANGELOG.md` under `[Unreleased]` for every meaningful change before merging to `main`.

### Roadmap
- `docs/ROADMAP.md` is the single canonical “where we are / what's next” document.
  At the start of any new session, read it first to load current state, the active
  track, and the backlog.
- When a tracked item ships, update `docs/ROADMAP.md` in the **same commit** as the
  `CHANGELOG.md` entry: move the item out of the roadmap, then refresh the
  “Current State” table (test count, coverage, branch) if those numbers changed.

### Performance Optimization
- Engage profiler/measurement workflow only for explicit performance tasks.
