# Changelog

All notable changes to this project are documented here.

---

## [Unreleased]

### Fixed (NQueen.Kernel — Unique Visualize path)
- **`BitmaskSolver.Unique.cs`** — `EnumerateUniqueVisualizeAdaptive` was visiting ~2×
  more nodes than necessary: a single full-board pass was used for both GUI animation
  and solution counting. Replaced with a two-phase approach matching the established
  `CollectAllSamplesAndCountParallel` pattern in All mode:
  - **Phase 1** — full-board animation DFS (`RestrictFirstCol: false`) so the GUI
    shows queens placed on any row in column 0; stops as soon as `cap` canonical
    samples are stored. Uses `IsIdentityCanonical` filter instead of the previous
    `HashSet<UInt128>` dedup, eliminating per-solution hash allocations.
  - **Phase 2** — `CountUniqueAdaptive` for the exact solution count via the
    half-board algorithm (~2× fewer nodes than the old full-board pass).
  Solution counts are unchanged; verified across N = 8–16.

### Performance (NQueen.Kernel)
- **`BitmaskSearchEngine.cs`** — replaced 18-line De Bruijn 64-bit lookup table with
  `BitOperations.TrailingZeroCount` JIT intrinsic (`TZCNT` on x64), eliminating a
  multiply + shift + array-index on every queen-placement candidate in the hot DFS loop.
  Also removes the 64-byte static `_deBruijnIndex64` array from the type's static state.
- **`BitmaskSearchEngine.cs`** — converted `SearchState` from `sealed class` to `struct`.
  All call sites already passed it via `ref`; the change eliminates one heap allocation
  per `Run()` call and improves cache locality of the DFS state machine fields.
- **`BitmaskSolver.cs`** — introduced `EnsureMinThreads()` with an `Interlocked`
  one-shot guard. `ThreadPool.SetMinThreads` was previously called on every large-board
  parallel solve (a permanent process-wide write); it now executes at most once per
  process lifetime.

### Fixed (NQueen.Kernel)
- **`BitmaskSolver.cs`** — removed redundant outer `lock (_sync)` in `GetSimResultsAsync`;
  `Solve()` already acquires the same lock, making the outer acquire a no-op reentrant
  acquisition.
- **`BitmaskSolver.cs`** — replaced `_scratchBuffer ?? new int[rows.Length * 8]` in
  `ConstructiveSampleSolutions` with `_scratchBuffer!`; the buffer is guaranteed non-null
  after `ResetForSolve()`, so the fallback allocation was unnecessary.

### Refactored (NQueen.Kernel)
- **`Usings.cs`** — removed `global using System;`, `global using System.Collections.Generic;`,
  and `global using System.Threading.Tasks;`; all three are already injected by
  `<ImplicitUsings>enable</ImplicitUsings>`.

---

### Added (NQueen.Benchmarking)
- **`ConsolePruningImpactBenchmarks.cs`** — two new benchmark classes
  (`ConsolePruningImpactAllBenchmark`, `ConsolePruningImpactUniqueBenchmark`) that measure
  the performance difference between the old Console solver configuration (no pruning,
  events on) and the new one (pruning on, events off, adaptive depth) across N = 12, 14, 16.
  Measured results on Intel i7-14700K / .NET 10.0.8:
  - Unique N=12: **−17%** (891 µs vs 1,076 µs)
  - Unique N=14: **−6%** (13.5 ms vs 14.4 ms)
  - Unique N=16: flat (within noise — parallel saturation dominates)
  - All mode N=12–16: flat (half-board restriction already dominates search reduction)
- **`Program.cs`** — default run updated to `ConsolePruningImpactAllBenchmark` +
  `ConsolePruningImpactUniqueBenchmark`.

### Fixed (NQueen.Console)
- **`DispatchCommands.cs`** — interactive solver branches now set `EnableEvents = false`
  (eliminates wasted event firing with no subscribers), `IsSolverCanceled = false` (prevents
  stale cancellation state on repeated runs), `EnablePrefixMinimalityPruning = true`,
  `EnablePartialReflectionPruning = true`, and `UseAdaptiveDepth = boardSize >= 14` —
  matching the GUI's kernel configuration for equivalent performance.
  Also switched bare `var solver` to `using var solver` to fix a resource leak.
- **`DispatchCommands.cs`** — All-mode interactive path now sets
  `EnableHalfBoardRestriction = boardSize >= 15`, consistent with the non-interactive path
  and the GUI.
- **`Program.cs`** — non-interactive solver block extended with `IsSolverCanceled = false`,
  `EnablePrefixMinimalityPruning = true`, `EnablePartialReflectionPruning = true`, and
  `UseAdaptiveDepth = size >= 14` to match the GUI's optimisation settings.

### Refactored (NQueen.Benchmarking)
- **File consolidation** — reduced 14 benchmark source files to 7 thematic files for
  improved readability and SRP compliance:
  - `SymmetryBenchmarks.cs` — merges `SymmetryHelperCanonicalFormBenchmark`,
    `SymmetryHelperCanonicalKeyBenchmark`, `SymmetryPackedBenchmarks`,
    `SymmetryPrunedUniqueCounterBenchmark` (4 old files → 1).
  - `UniqueModeBenchmarks.cs` — merges `UniqueSolutionCounterPackedBenchmark`,
    `CountUniqueFastHalfBoardBenchmark`, `CountUniqueHalfBoardBenchmarks`,
    `UniqueCountOnlyHighNBenchmark` (4 old files → 1).
  - `AllModeBenchmarks.cs` — merges `AllCountOnlyN18Benchmark`,
    `CombinedSolverBenchmarks` (All-mode half), `MediumPrefixPruningParallelBenchmark`
    (3 old files → 1); `AllPrefixPruningBenchmark` replaces
    `MediumPrefixPruningParallelBenchmark` with cleaner parameterised design.
  - `UniqueModeVariantsBenchmark.cs` — carries the Unique-mode half of
    `CombinedSolverBenchmarks` as a standalone file.
- **11 style/correctness issues resolved** across all benchmark files:
  - Fixed 3 misaligned closing braces (`SymmetryPackedBenchmarks.cs`,
    `CountUniqueHalfBoardBenchmarks.cs`, `UniqueCountOnlyHighNBenchmark.cs`).
  - Fixed `BitmaskSolver` resource leak in `SymmetryAddIfUniquePackedBenchmark`
    (`GlobalSetup` now uses `using var solver`).
  - Extracted `NoopFormatter` and replaced 12 `new SolutionFormatter()` instances in
    count-only benchmarks.
  - Removed all `SetSimulationToken` calls (redundant when `EnableEvents = false`).
  - Converted constructor-style solver init to object-initialiser style throughout.
  - Replaced `new int[…]` scratch allocations with `GetScratchBufferSize()`.
  - Changed `private int[] field = null!;` declarations to auto-properties.
  - Renamed `N` → `BoardSize` in `NQueenBench` for naming consistency.
  - Removed redundant `global using System` and `global using System.Linq` from
    `Usings.cs` (covered by `<ImplicitUsings>enable</ImplicitUsings>`).
- **`Program.cs`** — updated default `BenchmarkRunner.Run<>` reference from removed
  `UniqueCountOnlyHighNBenchmark` to consolidated `UniqueHighNBenchmark`.

---

## [1.0.0] — 2026-05-29  _(branch `refactor/consolidate` merged to `main`)_

### Fixed (NQueen.Console)
- **`Program.cs`** — `--halfboard` CLI flag no longer silently does nothing when `--mode`
  is not `all`. A yellow warning is now printed and the flag is cleared before the solver
  runs, making the ignored intent explicit to the user.

### Added (NQueen.Kernel — XML documentation)
- **`BitmaskSolver.cs`** — added `<summary>` XML doc comments to all 11 previously
  undocumented public configuration properties:
  `EnableEvents`, `AllStorageMode`, `UniqueStorageMode`, `UseCountOnlyUniqueMode`,
  `UseCountOnlyAllMode`, `UseParallel`, `ParallelRootSplitDepth`, `UseAdaptiveDepth`,
  `EnablePrefixMinimalityPruning`, `EnablePartialReflectionPruning`,
  `EnableHalfBoardRestriction`.
  Each comment describes the property's effect, default value, and any interactions with
  related properties or mode constraints.

### Fixed (CI)
- **`.github/workflows/ci.yml`** — rewrote the existing workflow to fix several bugs and
  improve reliability:
  - Added missing `: ` separators in `env:` values (`DOTNET_CLI_TELEMETRY_OPTOUT` and
    `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` were silently ignored before).
  - Removed `dotnet-quality: 'preview'` — .NET 10 is GA; the flag caused unnecessary
    pre-release SDK resolution.
  - Added explicit `dotnet build --configuration Release` step before test so
    `--no-build` in the test step is valid and faster.
  - Added `--no-build` and `--configuration Release` to `dotnet test`.
  - Excluded `NQueen.Benchmarking` from coverage collection (it is an exe, not a test
    assembly, and caused the test runner to fail trying to discover tests in it).
  - Extended `push` branch triggers to also cover `refactor/**`, `feature/**`, `fix/**`
    so CI runs on all active development branches, not just `main`.
  - Added `pull_request: branches: [main]` filter so PRs into other branches do not
    trigger unnecessary runs.
  - Added `retention-days: 14` to the coverage artifact upload.
  - Renamed workflow from `ci` to `CI` for display clarity.

### Fixed (project configuration)
- **`NQueen.ConsoleApp.csproj`** — changed `TargetFramework` from `net10.0-windows` to
  `net10.0`; the Console project has no Windows-specific dependencies (no WPF, no WinForms,
  no P/Invoke), so the `-windows` suffix was unnecessary and restricted portability.

### Removed (NQueen.Benchmarking dead template artefacts)
- **`Properties\Resources.resx`** and **`Properties\Resources.Designer.cs`** — deleted;
  these were left over from the project template and are never referenced by any benchmark.
- **`NQueen.Benchmarking.csproj`** — removed the dead `<EmbeddedResource>` entry that
  pointed to the now-deleted `Resources.resx`.

### Removed (NQueen.Benchmarking dead code)
- **`Usings.cs`** — removed dead `global using NQueen.Kernel;`; the root namespace has no
  public types after `BitboardNQueenSolver` moved to `NQueen.Kernel.Solvers`.
- **`AllCountOnlyN18Benchmark.cs`**, **`CountUniqueFastHalfBoardBenchmark.cs`**,
  **`CountUniqueHalfBoardBenchmarks.cs`** — removed three identical copy-pasted private
  `NoopFormatter` nested classes, replaced by a single shared top-level class.

### Added (NQueen.Benchmarking)
- **`NoopFormatter.cs`** — new shared `internal sealed class NoopFormatter` consolidating
  the three previously duplicated nested formatters.
- **`NQueenBench.cs`** — extracted `NQueenBench` benchmark class out of `Program.cs` into
  its own file; each benchmark class now lives in its own file.

### Fixed (NQueen.Benchmarking)
- **`Program.cs`** — removed `NQueenBench` class (moved to `NQueenBench.cs`); fixed
  corrupted `…` ellipsis character in the startup console message.
- **`UniqueSolutionCounterPackedBenchmark.cs`** — added `using var` to `BitmaskSolver`
  for consistent deterministic disposal, matching every other benchmark.
- **`SymmetryHelperCanonicalKeyBenchmark.cs`** — changed `int[]?` fields to `int[] = null!`
  and removed the unreachable null-guard inside `[Benchmark]` (`[GlobalSetup]` always runs
  before `[Benchmark]` in BenchmarkDotNet).
- **`CombinedSolverBenchmarks.cs`** — reduced `AllModeVariantsBenchmark` parameter space
  from 144 combinations (3×3×2×2×4) to 32 (2×2×2×4) by collapsing the two independent
  pruning booleans into a single `EnablePruning` flag and trimming the size/depth ranges to
  the two most representative values each.

### Removed (NQueen.Console dead code)
- **`CommandConstants.cs`** — entire file deleted; every constant was only referenced from
  `CommandProcessor` or `HelpCommands`, both of which are also removed.
- **`ConsoleUtils.cs`** — entire file deleted; `WriteLineColored` was only called from
  the dead `HelpCommands` class.
- **`HelpCommands.cs`** — entire file deleted; all members (`ShowHelp`, `ShowExitError`,
  `DumpAllHelp`, `DumpHelpText`, `Valid_Commands`, `Command_Example`, `NQueen_Help_Board_Size`,
  `NQueen_Solution_Mode`, `NQUEEN_BOARDSIZE`, `Bitmask_Help`, `Banner`) had zero live call sites.
- **`ICommandProcessor.cs`** — interface deleted; never resolved from DI at runtime.
- **`CommandProcessor.cs`** — class deleted; all three methods were deprecation-message stubs
  with no live callers.
- **`DispatchUtils.cs`** — entire file deleted; `ParseInput`, `CreateChessBoard`, and
  `LaunchConsoleMonitor` all had zero call sites.
- **`DispatchCommands`** — removed dead `RegexSpaces()`, `genRegEx()`, and `_whiteSpacesRegex`;
  `partial` modifier removed now that no source-generated partial members remain.
- **`App.cs`** — removed dead `DispatchCommands dispatchCommands` constructor parameter and the
  `_dispatchCommands` field; `RunInteractiveMenu` is `static` so the instance was never used.
- **`Program.cs`** — removed stale dotnet-counters installation comment and dead DI
  registrations for `DispatchCommands` (transient) and `ICommandProcessor`/`CommandProcessor`.
- **`Using.cs`** — removed three dead global usings: `NQueen.ConsoleApp.Interfaces`,
  `System.Diagnostics`, `System.Text.RegularExpressions`.

### Renamed
- **`DispachCommands.cs` → `DispatchCommands.cs`** — corrected long-standing filename typo
  (missing `t`). Class name was already correct; only the filename was misspelled.

### Fixed (accessibility)
- **`ChessboardUserControl.xaml`** — added `AutomationProperties.Name="Chessboard"` to the
  outer `Border` to satisfy the *NameNotNull* rule (Accessibility Checker error at line 16).
- **`ChessboardUserControl.xaml`** — bound `Width` and `Height` from `SquareViewModel` and
  added `AutomationProperties.Name="{Binding}"` to each DataTemplate `Border`, giving every
  chess square a concrete bounding rectangle and resolving the *BoundingRectangleNotNull* rule
  (Accessibility Checker error at line 36).
- **`ChessboardUserControl.xaml.cs`** — removed the now-resolved TODO comment block that
  tracked both accessibility errors.

### Removed (dead code)
- **`SimulationSettings`** — deleted 7 unused members:
  `ParallelAllAutoEnableThresholdN`, `LargeBoardProgressThrottleThreshold`,
  `DynamicRootSplitLimitN`, `AdaptiveRootMultiplier`, `RootBranchThreshold`,
  `WeightLookaheadDepth`, `CalculateSplitDepth`.
- **`BoardSettings`** — deleted 6 unused constants:
  `RelativeFactor`, `ExtraSmallSizeForUniqueMode`, `SmallSizeForUniqueMode`,
  `MediumSizeForUniqueMode`, `RelativeLargeSizeForUniqueMode`, `DefaultQueenImagePath`.
- **`MainViewModel.Properties.cs`** — removed unreachable private method `UpdateProgress(double, string)`.
- **`MainViewModel.Properties.cs`** — removed no-op `OnIsSingleRunningChanged` partial callback
  (the CommunityToolkit source generator already fires `PropertyChanged` automatically).
- **`LazyPositionList.cs`** — removed stale TODO comment; the class is already in use.
- **`HelpCommands.cs`** — removed dead `ProcessHelpCommand` method (zero call sites) and its
  two associated TODO comments; also removed orphaned `case` blocks left by the method removal.
- **`DispatchUtils.cs`** — removed two stale TODO comments (`"Change the order of indices"` and
  `"Find a better data structu"`) — the board rendering and variable naming are both correct.
- **`InputViewModel.cs`** — removed stale bug TODO; the described chessboard-reset issue is
  already resolved by `UpdateUiState()` calling `ChessboardVm?.CreateSquares(boardSize)` before
  every simulation.
- **`UiMessages.cs`** — removed stale TODO to move constants; the referenced constants are
  already present in `CommandConst`.

### Clarified
- **`ISolverBackEnd`** — replaced stale "moved from ISolver" inline comment with a clear
  description of `UseCountOnlyAllMode`/`UseCountOnlyUniqueMode` and their relationship to
  the solver's `ResultStorageMode` properties.

### Refactored
- **`BitboardNQueenSolver` relocated** — moved from `NQueen.Kernel/BitboardNQueenSolver.cs`
  (namespace `NQueen.Kernel`) to `NQueen.Kernel/Solvers/BitboardNQueenSolver.cs`
  (namespace `NQueen.Kernel.Solvers`), co-locating it with `BitmaskSolver` and all other
  solver infrastructure. No call-site changes required; the existing
  `global using NQueen.Kernel.Solvers;` in `NQueen.Kernel/Usings.cs` already covers all callers.
- **`ISolverFrontEnd` TODO removed** — deleted the stale five-line TODO comment block from
  `NQueen.Domain/Interfaces/ISolverFrontEnd.cs`. The described work (migrate event-arg types
  to `Memory<int>` and add a `Guid`-bearing progress event) had already been fully implemented
  in `QueenPlacedEventArgs`, `SolutionFoundEventArgs`, and `ProgressUpdateEventArgs`.

### Verified
- Build: ✅ 0 errors, 0 warnings
- Tests: ✅ 304/304 passing
