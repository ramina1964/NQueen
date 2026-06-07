# Changelog

All notable changes to this project are documented here.

---

## [Unreleased]

### Docs
- **`docs/EVENT-MIGRATION-PLAN.md`** — new design document specifying a staged migration of the
  solver's `event` surface (`QueenPlaced` / `SolutionFound` / `ProgressValueChanged` +
  `SetSimulationToken` + `IsSolverCanceled`) to per-call push sinks (`IProgress<T>` + a conflating
  `Channel<T>` for the high-frequency animation stream + `CancellationToken`). Includes an as-built
  inventory, a behaviour-preserving **Stage 0** notification-seam extraction (which also resolves
  an `EnableEvents` gate inconsistency on the terminal 100% progress raises), a six-stage rollout,
  a risk table, a worth-it analysis, and a **§1a pre-work audit** to verify whether the existing
  manual leak mitigation (symmetric unsubscribe + nulling + `IDisposable`) removes the
  lapsed-listener leak correctly and completely before the migration begins.
- **`docs/ROADMAP.md`** — added a "Design docs awaiting execution" pointer under *Next session —
  start here* linking `EVENT-MIGRATION-PLAN.md`, noting it belongs on its own `refactor/solver-sinks`
  branch after the `test/suite-review` Fact→Theory consolidation merges.

### Changed (NQueen.UnitTests)
- **Fact→Theory test consolidation (coverage-preserving)** — merged near-identical `[Fact]`
  methods (and `[Fact]` methods that looped internally over inputs) into parameterised
  `[Theory]` + `[InlineData]` cases across six files, reducing test-method count while keeping
  every input scenario as a visible, individually-reported case:
  - `DomainUtilityTests.cs` — `IntArrayStructuralComparer.Equals` (3→1), `MemoryIntArrayComparer.Compare`
    (4→1 via `Math.Sign`, plus a literal duplicate removed), `GetAllFast`/`GetUniqueFast` count lookups (Theory-merged).
  - `SymmetryHelperExtendedTests.cs` — `ApplyAdvancedSymmetryPruning` "mask unchanged" (2→1) and
    "Column 0 cuts to half" (2→1).
  - `SearchHelpersTests.cs` — `ShouldPrunePrefixFull` (4→1; rotate-180 regression guard kept separate).
  - `BitmaskSolverAllModeTests.cs` — folded the internal `foreach {2,3}` zero-solution Fact into the
    existing `AllMode_CountOnly_SmallN` Theory.
  - `BitmaskSolverUniqueTests.cs` — `UniqueMode_NoSolutionExists` internal-`foreach` Fact → Theory(2,3).
  - `BitmaskSolverCountUniqueTests.cs` — three `PreservesPruningFlags` Facts → one Theory.
  - `SolutionFormatterTests.cs` — zero-/one-based formatting (2→1).
  - `BitboardNQueenSolverTests.cs` — out-of-range throw cases (2→1).
  - Heterogeneous-assertion and distinct-routing-branch Facts were intentionally left as Facts.
    Fast suite stays green: 424 unit + 89 view-model tests pass.

### Added (NQueen.GUI)
- **`AppStyles.xaml` `PanelCardStyle`** — a re-templated `GroupBox` that replaces the Win32
  etched frame with a flat, square card: a bold header band (with a 1px bottom separator)
  over a content row, content padding owned by the template (`Padding` setter =
  `PanelContentMargin`), and predictable sizing (no header notch). It stays a `GroupBox` so
  the `Header` still labels the group for UI Automation / screen readers. A small brush
  palette (`PanelBackgroundBrush`, `PanelBorderBrush`, `AccentBrush`, `ProgressBrush`,
  `SliderTrackBrush`) was added as the single source of truth for colour, and the existing
  `ButtonStyle` / `ProgressBarStyle` / `SliderStyle` were wired to it (appearance-neutral).
- **`NumericUtils.FormatWithSpaceSeparator(ulong)`** — a `ulong` overload mirroring the
  existing `long` one, so the (unsigned) solution count can be formatted with space
  thousands-separators without a lossy cast.
- **`AppStyles.xaml` colour/typography tokens** — the brush palette gained `SurfaceBrush`
  (White), `TextPrimaryBrush` (Black), `TextMutedBrush` (Gray), `TextSubtleBrush`
  (DarkSlateGray), `SelectionForegroundBrush` (Crimson), and the error trio
  `ErrorBorderBrush` / `ErrorBackgroundBrush` / `ErrorForegroundBrush` (the exact literals
  previously inlined in `LabelErrorStyle`), plus a `CaptionFontSize` (`sys:Double` = 11)
  typography token. These are now the single source of truth for the per-view colours and
  caption sizes that used to be hard-coded.

### Changed (NQueen.GUI)
- **XAML magic-constant cleanup (appearance-neutral)** — every literal colour and caption
  `FontSize` across the seven view XAMLs was routed through the new `AppStyles.xaml`
  brushes / `CaptionFontSize` token (e.g. `Background="White"` → `{StaticResource
  SurfaceBrush}`, `Foreground="Black"` → `{StaticResource TextPrimaryBrush}`, the list
  selection `Crimson` → `{StaticResource SelectionForegroundBrush}`, the two `FontSize="11"`
  captions → `{StaticResource CaptionFontSize}`), and `LabelErrorStyle` was rewired onto the
  error brushes. All values are byte-identical, so rendering is unchanged.
- **Panel rollout to `PanelCardStyle`** — `InputPanelUserControl`, `OutputPanelUserControl`,
  `SimulationPanelUserControl`, `AdvancedSettingsPanel` and the `ActiveSolutionUserControl`
  header all moved from inline GroupBox chrome (`Background`/`BorderBrush`/`BorderThickness`/
  `Padding` literals + an inner `Grid Margin`) to the shared card style. `ActiveSolution`
  keeps its White/Gray look via local instance values; the now-redundant rounded
  `WhiteSmoke` header `Border` in `MainWindow.xaml` was made layout-only. `AdvancedSettingsPanel`
  was also normalised to the `LabelCellMargin` / `InputCellMargin` spacing tokens.
- **Solution-count formatting** — the three `NoOfSolutions` assignment sites
  (`MainViewModel.Commands.cs` ×2, `MainViewModel.cs`) now use
  `NumericUtils.FormatWithSpaceSeparator` (space groups) instead of `:N0` (culture comma),
  matching `MemoryConsumption`.
- **`OutputPanelUserControl.xaml` (Solution Summary)** — both the descriptive labels and
  their values are now right-justified: the label column stretches (`*`, right-aligned) so
  each caption sits directly left of its value, and the value column is `Auto`/right-aligned
  flush to the panel edge. A hidden width-sizer `Label` holding the largest representable
  count (`18 446 744 073 709 551 615`) pins the value column wide enough that no count clips,
  even for All-mode at high N.
- **`MainWindow.xaml` right control column** — widened from `300` to `420` (design canvas
  `1140` → `1260`) so the Solution Summary no longer clips at high N. The four panels share
  one grid column, so they expand together with right edges aligned. The column was
  restructured into a space-between grid (`Auto` panel rows separated by `*` gap rows) and
  `MainWindow.xaml.cs` `ApplyDesignLayout` now sets `controlColumn.MinHeight` to
  `DesignBoardSize` (mirroring `solutionList.Height`), so the panels plus the inter-panel
  gaps total at least the chessboard height with tops and bottoms aligned, and the column
  grows rather than clipping when content overflows.

### Fixed (NQueen.GUI)
- **`MainWindow.xaml` header overflow at high N** — for long selected-solution strings
  (e.g. N > 25 in Single mode) the header `GroupBox` used to stretch and clip every panel.
  The header (`ActiveSolutionUserControl`) previously sat in row 0 spanning all five body
  columns (`Grid.ColumnSpan="5"`), so its content's desired width inflated the shared `Auto`
  board / solution-list columns. The layout is now an outer two-row grid: the header alone in
  row 0 at the fixed canvas width, and the five-column body nested in row 1 — so the header
  can no longer affect the body columns.
- **`ActiveSolutionUserControl.xaml` long-detail scrolling** — with the header width now
  fixed, a solution string wider than the header surfaces a horizontal scrollbar at the
  bottom of the card (the `ScrollViewer` is `HorizontalScrollBarVisibility="Auto"` over
  `NoWrap` text, switched to `VerticalAlignment="Stretch"` so the bar sits at the bottom
  edge) instead of widening the window.
- **`SimulationPanelUserControl.xaml` / Solver Settings clipping** — the Simulation card's
  `MinHeight="100"` forced it taller than its content (the progress bar is hidden when idle),
  which starved the Solver Settings card below it and clipped its bottom. Removing the
  `MinHeight` lets the Simulation card shrink to its content; the control column's
  space-between `*` rows redistribute the freed height so Solver Settings is no longer clipped.
- **Solver Settings clipping (true root cause) + reclaimed Simulation height** — two issues
  the `MinHeight` removal above did not fully resolve. (1) `ApplyDesignLayout` pinned
  `controlColumn.Height` to an **exact** `DesignBoardSize` (640), so when the four panels'
  natural height exceeded 640 — most notably when the over-max board-size error label
  appears — the bottom Solver Settings card was clipped, and because the `Viewbox` merely
  scales whatever it is given, maximizing/resizing could not recover the clipped content.
  The pin is now `controlColumn.MinHeight = DesignBoardSize`, so the column **grows** to fit
  its content (still bottom-aligning to the board when it fits, via the `*` gap rows) and the
  `Viewbox` scales the full, unclipped composition. (2) The Simulation card was still taller
  than its visible content because the progress bar/label bound to `Visibility.Hidden` when
  idle, which **reserves** layout space. All idle-state assignments of `ProgressVisibility` /
  `ProgressLabelVisibility` (their defaults in `MainViewModel.Properties.cs` plus the sites in
  `MainViewModel.Commands.cs`, `MainViewModel.cs`, and `MainViewModel.Events.cs`) were
  switched from `Hidden` to `Collapsed`, so the progress row consumes no height when idle and
  the freed space is given to Solver Settings (the active-run `Visible` state is unchanged).
- **`ListOfSolutionsUserControl.xaml` / `MainWindow.xaml`** — the solution-list frame no
  longer "jumps" from a collapsed height to its full height as the first ~5 results arrive.
  The grouping `Border` previously sized to its content (with the inner `ListBox` capped at
  `MaxHeight="130"`); it now stretches (`VerticalAlignment="Stretch"`) to fill the
  board-height area the control is already allocated, and the `ListBox` fills that fixed
  frame, so the height is stable from the first result. Spacing was moved inside the frame
  (Border `Padding` token = 4, outer `Margin="0"`). The list also hugs its content width
  (`HorizontalAlignment="Left"`, `MaxHeight` and `MinWidth` removed) and the left grid
  column is now `Width="Auto"`, so it no longer reserves surplus horizontal space. The
  `Border` and `ListBox` widths are
  now capped (`MaxWidth`) to a hidden sizer `TextBlock` measuring the widest item
  (`Solution No. 00`) in the selected-item weight (Bold), so the frame is exactly as wide as
  the solution name — font/DPI-robust, with no hard-coded pixel width.
- **`MainWindow.xaml` constant right-column gaps during simulation** — the four right-column
  cards previously used `*` (space-between) spacer rows that distributed surplus into even
  gaps when idle but collapsed to zero while simulating (the panels grew to fill the column),
  so the cards touched with no gap during a run. The three spacers are now fixed `8px` rows
  (matching the app's standard spacing unit), so each card keeps its natural `Auto` height
  plus a constant gap at all times; the column's `MinHeight` still lets it grow when content
  overflows.

### Removed (NQueen.GUI)
- **Dead code purge** — deleted five never-referenced types (`Utils/LayoutUtils.cs`, the
  custom `Converters/BooleanToVisibilityConverter.cs`, `EnumDescriptionConverter.cs`,
  `FirstValidationErrorConverter.cs`, `RatioConverter.cs`), each verified unused solution-wide
  including XAML `{StaticResource}` usage and the test projects. The three live converters
  (`DisplayModeToEnabledConverter`, `NullImageConverter`, `StringNotEmptyToVisibilityConverter`)
  are retained.
- **Legacy messaging folders** — removed the build-excluded `Messaging/` and `MessagePruning/`
  folders (6 stale `.cs` files) and the now-orphaned `<Compile Remove="…/**/*.cs" />` item
  group from `NQueen.GUI.csproj`.
- **`App.xaml`** — dropped the dead `BooleanToVisibilityConverter` resource (its
  `StaticResource` key was never consumed; the entry resolved to the built-in WPF type) and
  the now-unused `xmlns:converters` namespace declaration.
- **`AppStyles.xaml`** — removed the unused `PanelStackGap` spacing token (defined but never
  referenced).

### Fixed (NQueen.Kernel — duplicate lookup-materialize samples for All & Unique, N >= 21)
- For N >= 21 in **All** or **Unique** mode with **Materialize** storage, the total count was
  correctly served from the `ExpectedSolutionCounts` lookup table, but the displayed sample
  solutions were wrong. `SampleMaterializeUsingLookup` unconditionally routed N >= 21 (always
  >= `ConstructiveSampleThresholdN` = 20) into `ConstructiveSampleSolutions`, which built a
  single base placement plus its rotations/reflections. In **All** mode those symmetry
  variants share one canonical key, so `GetCanonicalKey` collapsed them to **5 identical
  boards**; in **Unique** mode the variants were all orientations of the **same fundamental
  solution**. The dead `else` branch below the constructive guard (a capped
  `BitmaskSearchEngine.Run`) was unreachable because `LookupThresholdN` (21) >
  `ConstructiveSampleThresholdN` (20).
- `SampleMaterializeUsingLookup` now runs an **early-exit DFS** that collects up to the
  display cap of *genuinely distinct* solutions then stops — exactly the requested behaviour:
  search until the cap is reached, save those samples, stop, and report the total via the
  lookup table. It reuses the proven collectors already used on the N = 14..20 materialize
  paths: `CollectAllSampleSolutionsDFS` (All) and the canonical `CollectUniqueSamplesDFS`
  (Unique). The now-dead `ConstructiveSampleSolutions` and `GenerateSymmetryVariants` helpers
  were removed (`GenerateConstructiveSolution` is kept — Single mode still uses it).
- `CollectUniqueSamplesDFS` now stores raw rows in `_largeBoardRawSolutions` for N > 25
  (mirroring `CollectAllSampleSolutionsDFS`); previously it only stored a packed canonical key,
  which is 0 for N > 25 and is skipped by `BuildResults`, so Unique samples for N = 26..29
  would not surface.
- Tests: `BitmaskSolverMaterializeTests.Materialize_DistinctSamples_AreReturned` was widened to
  a `[Theory]` asserting distinct samples for **both** All and Unique at N = 21 (it previously
  exercised only Unique and documented the All-mode collapse as expected). All
  `BitmaskSolverMaterializeTests` (8) and the high-board / large-board count suites pass.
- Tests (correctness guard): added rigorous Unique-mode coverage. A new
  `UniqueMode_Materialize_SamplesAreCanonicalAndFundamentallyDistinct` `[Theory]` (N = 21..25,
  the whole GUI Unique range) asserts every sample is a canonical representative
  (`SymmetryHelper.IsIdentityCanonical`) **and** that no two samples share a canonical
  signature (`GetCanonicalForm`) — i.e. they are genuinely different fundamental solutions, not
  rotations/reflections of one another (the precise failure mode of the old sampler).
  `Materialize_DistinctSamples_AreReturned(Unique)` now also checks canonical-distinctness, and
  the All-mode test guards against the 5-identical-boards regression. Suite grew from 8 to 13
  cases, all green.

### Changed (NQueen.GUI)
- **`MainWindow.xaml` / `MainWindow.xaml.cs`** — the main window is now user-resizable.
  The root layout is wrapped in a `Viewbox` (`Stretch="Uniform"`) and the window switched
  from `ResizeMode="NoResize"` + `SizeToContent="WidthAndHeight"` to `ResizeMode="CanResize"`
  with a base size of `1200x780` and a `820x560` floor. The whole UI now scales uniformly
  (the chessboard stays square; panels keep their proportions) and the window letterboxes
  when its aspect ratio differs from the design ratio.
  The code-behind shrank from 227 to ~107 lines: the monitor-fit board arithmetic, the
  `OnDpiChanged`/`LocationChanged` re-layout plumbing, and the `user32` P/Invoke
  (`MonitorFromWindow`/`GetMonitorInfo`) were removed in favour of a single one-time
  `ApplyDesignLayout` at a fixed `DesignBoardSize` (the Viewbox handles on-screen and DPI
  scaling). This also removed a latent crash: with `Content` now the `Viewbox`, the former
  `(Grid)Content` cast would have thrown.
- **`app.manifest`** — added an application manifest declaring Per-Monitor V2 DPI awareness
  (`dpiAwareness` = `PerMonitorV2, PerMonitor`, with the legacy `dpiAware` = `true/pm`
  fallback) and wired it via `<ApplicationManifest>` in `NQueen.GUI.csproj`. The window now
  re-renders crisply when dragged between monitors with different scale factors (e.g. 100%
  laptop to 150% external) instead of relying on WPF's System-aware default, which
  bitmap-stretches on the secondary monitor. Complements the `Viewbox` layout scaling, which
  is independent of DPI awareness.
- **Spacing system** — introduced a single source of truth for layout spacing on a 4px grid
  (`AppStyles.xaml` `Thickness` tokens: `PanelContentMargin` 8, `FramePadding` 4,
  `ButtonMargin` 8, `PanelStackGap` 0,8,0,0, `FieldRowMargin` 0,4, `LabelCellMargin` 0,4,8,4,
  `InputCellMargin` 0,4,0,4). The Input, Output, Simulation, Active-solution, Solver-settings
  and solutions-list panels now reference these tokens instead of ad-hoc literals
  (previously a mix of 2/3/5/6/8/10). The `MainWindow` right-hand control column was
  simplified from a 7-row layout with hard-coded 2px spacer rows to a 4-row stack using a
  consistent `PanelStackGap`.
- **Right control column width** — narrowed the `MainWindow` control column from `400` to
  `300` and reduced the Viewbox design canvas from `1240` to `1140` to absorb the freed space
  (no empty right band; the Simulate-resize fix is preserved). The Input, Output and
  Solver-settings panels switched their input/value columns from `*` (stretch) to `Auto`, so
  controls now sit one standard `LabelCellMargin` gap after their labels instead of being
  pushed to the far panel edge. The `ProgressBarStyle` lost its hard-coded `Width="310"` and
  now stretches to the (narrower) panel. All four GroupBoxes remain equal width.

### Fixed (NQueen.GUI)
- **`MainWindow.xaml` / `ActiveSolutionUserControl.xaml`** — clicking **Simulate** no longer
  appears to resize the window. A `Viewbox` measures its child at infinite size, so the root
  `Grid`'s content-driven natural size determined the uniform scale: when Simulate populated
  the "Selected Solution" locations text, the canvas grew and the whole UI zoomed (which
  looks identical to a resize, though the OS window bounds never changed). The Viewbox child
  now has a fixed design `Width="1240"`, and the header details `TextBlock` uses
  `TextWrapping="NoWrap"` so long location strings scroll horizontally inside the existing
  `ScrollViewer` instead of changing the canvas size. The scale is now constant regardless of
  content.
- **`MainWindow.xaml` / `ChessboardUserControl.xaml`** — the three middle-row columns
  (solution list, chessboard, control panels) now align at the top, and the gaps on the left
  and right of the chessboard are equal. The chessboard `Border` carried a `Margin="2"` that
  pushed its top/sides 2px in relative to the neighbouring frames, and the right-hand control
  column carried an extra `Margin="8,0,0,0"` on top of the grid's 10px gap column (making the
  right gap ~8px wider than the left). Both stray offsets were removed; the grid's two 10px
  gap columns are now the sole source of horizontal spacing.

### Removed (NQueen.GUI)
- **`AppStyles.xaml`** — deleted the unused `GroupBoxStyle` (every `GroupBox` set its
  properties inline, so the style — including a stale `Margin="5,0,0,0"` — never applied).

### Performance (NQueen.Kernel)
- **`BitmaskSolver.CountUnique.cs`** — tightened the prefix-prune gate in the
  `CountCanonicalDFS` hot loop so `SearchHelpers.ShouldPrunePrefixFull` is only invoked when
  reflection pruning is enabled. The loop-invariant `reflectionEnabled` flag is now tested
  first (`reflectionEnabled && col >= pruneDepthGate && …`), short-circuiting the call
  entirely on the reflection-off path and making the gate self-documenting. Behaviour is
  unchanged when reflection pruning is on (the production/benchmark configuration), so the
  unique count-only path is provably identical — verified by the existing exact-count tests
  (e.g. N=16 = 1 846 955). Measured on an isolated N=16/N=17 CPU benchmark the change is
  within run-to-run noise (no regression; the hot path remains ~97 % self-CPU in
  `CountCanonicalDFS`), so this is adopted as a correctness-neutral, low-risk cleanup rather
  than a speedup.

### Added (NQueen.Benchmarking)
- **`UniqueFastHalfBoardEvenOddBenchmark`** — isolated CPU benchmark pinned to N=16 (even)
  and N=17 (odd) that drives the Unique count-only `CountUniqueFastHalfBoard` →
  `CountCanonicalDFS` hot path directly. Added because the existing
  `UniqueFastHalfBoardBenchmark` `[Params(15, 16, 17)]` case aborts on N=15 (which routes
  through the separate `BitmaskParallelEngine.RunUnique` path, not the half-board DFS),
  producing `NA` and no usable baseline. This harness is the reusable measurement artifact
  for future kernel hot-loop work on this path.

### Fixed (NQueen.ViewModelTests)
- **`ProgressRelayTests.Heartbeat_ShouldSyntheticAdvance_WhenNoRealProgress`** — replaced a
  fixed `await Task.Delay(150)` with `TestHelpers.WaitForConditionAsync(() => vm.IsSimulating, …)`.
  The hard-coded delay raced with the async `SimulateCommand` start on slow CI runners,
  intermittently asserting `IsSimulating == true` before the simulation had begun
  (observed failing the PR gate while passing locally). Polling the actual condition makes
  the test deterministic.

### Added (Tooling)
- **`Fast.runsettings`** — opt-in test run settings that exclude the
  `[Trait("Category", "Slow")]` enumeration tests (N=13–15 full counts), letting the
  fast suite (~390 tests) run in a few seconds locally. Select it via *Test → Configure
  Run Settings* in Visual Studio or `dotnet test --settings Fast.runsettings`. Does not
  affect CI or unfiltered runs. README updated with usage.

### Fixed (CI)
- **`.github/workflows/ci.yml`** — split CI into two jobs so pull requests are no longer
  blocked by slow coverage instrumentation. Coverlet's line instrumentation over the
  recursive solver hot paths inflated an ~8s test suite to ~45 min on the 2-core runner,
  so PR checks were timing out in practice. Now a fast **`build-test`** gate builds and
  runs the non-Slow tests **without coverage** (PRs go green in a couple of minutes, with a
  `timeout-minutes: 15` safety net), while a separate, non-blocking **`coverage`** job
  (runs only on push to `main` or manual `workflow_dispatch`, `continue-on-error: true`)
  produces the Cobertura + HTML report and uploads the artifact.
- Earlier fix retained: the coverage step uses the `coverlet.collector` driver
  (`--collect:"XPlat Code Coverage"` + cobertura via `DataCollectionRunSettings`) instead
  of the unsupported coverlet.msbuild `/p:` flags, so `coverage.cobertura.xml` is actually
  produced and ReportGenerator finds it.

### Fixed (NQueen.GUI)
- **`MainWindow.xaml`** — changed `SizeToContent` from `Width` to `WidthAndHeight` and
  the main content `RowDefinition` from `Height="*"` to `Height="Auto"` so WPF measures
  the full content height at startup; bottom panels (Simulation, Solver Settings) are no
  longer clipped regardless of board size or display mode. Removed the interim `MinHeight`
  workaround.
- **`ListOfSolutionsUserControl.xaml`** — added `MaxHeight="130"` to the `ListBox` to
  cap the solution list at ~5 visible items (scrollbar appears when needed); corrected
  `Border` and inner `Grid` from `VerticalAlignment="Stretch"` to `VerticalAlignment="Top"`
  so the list stays top-aligned after the layout change.

### Refactored (NQueen.Kernel)
- **`BitmaskSolver.cs`** — split two cohesive method groups out into new partial-class files,
  reducing the file from 710 lines to 268 lines:
  - **`BitmaskSolver.CountUnique.cs`** — `CountUniqueAdaptive` and `CountUniqueFastHalfBoard`;
    the two unique count-only algorithms now live together in one focused file.
  - **`BitmaskSolver.Materialize.cs`** — `SampleMaterializeUsingLookup`,
    `ConstructiveSampleSolutions`, `GenerateConstructiveSolution`, `GenerateSymmetryVariants`;
    all sample-materialisation helpers grouped in one place.
  - `BitmaskSolver.cs` retains constructors, public API, `Solve`, `HandleModeCommon`,
    private fields, `ResetForSolve`, `BuildResults`, `ValidateRows`, `Dispose`, `EnsureMinThreads`.
  No behaviour changes; 430/430 tests pass.

### Added (NQueen.ViewModelTests)
- **`MainViewModelPositiveTests.cs`** — added `SaveSimulationResultsCommand_ShouldWriteContentViaService`:
  verifies that executing `SaveCommand` with a ready view-model invokes `ISaveFileDialogService`,
  writes non-empty content, and includes the board size and solution mode in the saved text.
  Uses the existing `MockSaveFileDialogService`; closes the long-standing TODO comment.

### Added (NQueen.UnitTests)
- **`BitmaskSolverCountUniqueTests.cs`** — 14 fast tests targeting the previously
  thinly-covered `BitmaskSolver.CountUnique.cs` (15 % line / 3 % branch baseline).
  Drives `CountUniqueAdaptive` through the public `ISolverBackEnd.GetSimResultsAsync`
  API with `SolutionMode.Unique` + count-only storage, covering the
  `BitmaskParallelEngine.RunUnique` branch (N = 1..9), the `CountUniqueFastHalfBoard`
  branch (N = 16, pruning-flag preservation), the save/restore semantics for
  `EnablePrefixMinimalityPruning` and `EnablePartialReflectionPruning`, and the
  `UniqueStorageMode = CountOnly` path. Full class runs in ~1.1 s.
- **`BitmaskSolverSingleModeTests.cs`** — 17 fast tests targeting
  `BitmaskSolver.Single.cs` (41 % line / 25 % branch baseline). Covers each routing
  branch in `SolveSingleMode`: curated fast path (N = 1, 4, 5, 8, 11, 13),
  empty-curated fall-through to fallback enumeration (N = 2, 3 — no solution),
  fallback enumeration for small N below `LargeBoardIntermediateStartSize`
  (N = 14), constructive path for N ≥ `LargeBoardIntermediateStartSize`
  (N = 15, 16, 17), engine-backed visualize branch with events, in-flight
  cancellation via the `IsSolverCanceled` flag, packed-storage materialisation
  (N ≤ 25), solver-state reset across consecutive runs, and the `enableCap=false`
  constructor overload. Full class runs in ~0.6 s.
- **`BitmaskSolverAllModeTests.cs`** — 17 fast tests (12 declarations + 5 Theory
  expansions) targeting `BitmaskSolver.All.cs`. Drives `RunAllUnified`,
  `EnumerateAllAdaptive`, `CollectAllSamplesAndCountParallel`, and
  `CollectAllSampleSolutionsDFS` through the public `ISolverBackEnd.GetSimResultsAsync`
  API, covering each routing branch: count-only small-N path through
  `BitboardNQueenSolver.CountSolutions` (N = 1, 4, 5, 6, 7, 8, 9; plus zero-solution
  N = 2, 3), small-N materialize path through `RunAllUnified` (N = 8),
  two-phase materialize path through `CollectAllSamplesAndCountParallel` at
  `N = ParallelAllMaterializeAutoEnableThresholdN` (= 14), count-only at N = 14,
  `CollectAllSampleSolutionsDFS` cap-stop semantics, event emission with post-cap
  suppression, in-flight cancellation via the `IsSolverCanceled` flag,
  `AllStorageMode = CountOnly` equivalence with `UseCountOnlyAllMode`,
  solver-state reset across consecutive runs, and the `enableCap=false`
  constructor overload. Full class runs in ~0.75 s.
- **`BitmaskSolverUniqueTests.cs`** — 15 fast tests (11 declarations + 4 Theory
  expansions) targeting the materialize path of `BitmaskSolver.Unique.cs`. Drives
  `ExecuteUniqueModeUnified` and `EnumerateUniqueVisualizeAdaptive` through the
  public `ISolverBackEnd.GetSimResultsAsync` API, covering each routing branch:
  small-N branch (N = 1, 4, 5, 6, 7, 8, 9 — `BitmaskSearchEngine.Run` with
  `RestrictFirstCol: true` and `IsIdentityCanonical` filter), zero-solution
  N = 2, 3, mid-N branch at `N = LargeBoardSymmetryPruningThreshold` (= 15) routing
  through `Engines.SymmetryPrunedUniqueCounter.Count`, large-N two-phase branch at
  `N = UniqueCountOnlyParallelThresholdN` (= 16) using `CollectUniqueSamplesDFS`
  + `CountUniqueFastHalfBoard` (asserting the exact OEIS A002562 count of
  1 846 955), `CollectUniqueSamplesDFS` cap-stop semantics,
  visualize-path event emission, in-flight cancellation, solver-state reset
  across consecutive runs, and the `enableCap=false` constructor overload. Full
  class runs in ~1.6 s. Count-only Unique routing remains covered by
  `BitmaskSolverCountUniqueTests`.
- **`SearchHelpersTests.cs`** — 5 new tests for the stateless reflection-only
  `SearchHelpers.ShouldPrunePrefixFull` helper: reflection-disabled no-op,
  prune-when-reflection-smaller, no-prune-when-identity-wins, negative-row guard,
  and an explicit regression guard asserting the helper does **not** apply the
  unsound rotate-180 minimality prune that caused the N >= 16 under-count.
- **`BitmaskSolverMaterializeTests.cs`** — 7 fast tests (10 with Theory expansions)
  targeting `BitmaskSolver.Materialize.cs`, the last `BitmaskSolver.*.cs` partial
  without a dedicated class. Drives `SampleMaterializeUsingLookup`,
  `ConstructiveSampleSolutions`, `GenerateConstructiveSolution`, and
  `GenerateSymmetryVariants` through the public `ISolverBackEnd.GetSimResultsAsync`
  API at `N >= LookupThresholdN` (21), where the count is served from the lookup
  table and samples are built constructively (no DFS). Covers both constructive
  special-case branches — N = 21 (`n % 6 == 3`) and N = 26 (`n % 6 == 2`) — in All
  and Unique modes, asserting the curated count and that every materialised sample
  is a conflict-free placement; the display cap (default and explicit) is honoured;
  Unique-mode samples are distinct (raw `int[]` storage exercises
  `GenerateSymmetryVariants`); and solver state resets across consecutive runs.

### Fixed (NQueen.Kernel — invalid constructive placement for n % 6 ∈ {2, 3})
- **`BitmaskSolver.Materialize.cs`** — rewrote `GenerateConstructiveSolution` to the
  canonical closed-form explicit construction keyed on `n mod 6`. The previous
  `n % 6 == 2` and `n % 6 == 3` special-case branches both emitted placements with a
  diagonal conflict (e.g. N = 15 and N = 20 placed two queens on a shared
  anti-diagonal). Because `ValidateRows` only checks row-array length — not diagonal
  legality — the invalid boards were surfaced silently through the constructive
  Single-mode path (N = 15, 21, 27) and the lookup-materialize sample path
  (N = 21, 26, 27). The corrected algorithm lists the even rows then the odd rows,
  moving `2` to the end of the evens and `1, 3` to the end of the odds for
  `n % 6 == 3`, and swapping `1`/`3` then moving `5` to the end of the odds for
  `n % 6 == 2`; output for every other remainder is unchanged ("evens then odds").
  Verified conflict-free for N = 8, 9, 14, 15, 20 and through the public API by the
  now-strict `SingleMode_ConstructivePath_ReturnsValidSolutionWithoutEnumeration`
  (N = 15, 16, 17) and the new `BitmaskSolverMaterializeTests` (N = 21, 26).

### Fixed (NQueen.Kernel — Unique mode count under-report at N >= 16)
- **`BitmaskSolver.CountUnique.cs` / `Engines/SearchHelpers.cs`** — corrected a
  silent under-count in `CountUniqueFastHalfBoard` that returned 692 857 instead
  of the OEIS A002562 value of 1 846 955 for N = 16 (and was wrong for N = 17..20).
  Root cause: the shared forward-prefix prune applied a rotate-180 "minimality"
  test that compares `rows[i]` against `N-1-rows[depth-i]` — i.e. against columns
  not yet fixed at the current depth — which is unsound as a forward-prefix prune
  and discarded branches that still extend to valid canonical solutions. The
  defect was previously invisible because the only other consumer
  (`SymmetryPrunedUniqueCounter`) runs with an effectively disabled prune gate at
  its sole reachable size (N = 15), and no N >= 16 test asserted the exact count.
  Fix: `SearchHelpers.ShouldPrunePrefixFull` is now reflection-only (horizontal
  reflection is the only sound forward-prefix prune; full canonicality across all
  eight symmetries is still enforced exactly by `IsIdentityCanonical` at the leaf).
  Both consumers were updated and the dead minimality parameter removed. Verified
  by the now-exact `UniqueMode_Materialize_N16_RoutesThroughTwoPhasePath`
  assertion and new `SearchHelpersTests` coverage.

### Performance (NQueen.Kernel — Unique count-only depth-2 parallelisation)
- **`BitmaskSolver.CountUnique.cs`** — replaced the coarse root-row partitioning in
  `CountUniqueFastHalfBoard` (which produced only ~(N+1)/2 uneven `Partitioner` ranges,
  ≈ 8–10 work items, leaving cores idle on tail imbalance) with **depth-2 work items**:
  one item per valid (column-0, column-1) queen pair, with column 0 restricted to the
  top half. For N = 20 this yields ~180 fine-grained items instead of ~10, giving far
  better core saturation and load-balancing. The former closure DFS is extracted to a
  reusable `CountCanonicalDFS` method driven by `Parallel.ForEach` with
  `localInit`/`localFinally` per-thread `rows`/`scratch` buffers (rented from
  `ArrayPool<int>`). The visited leaf set and the `IsIdentityCanonical` leaf filter are
  unchanged, so the canonical count is provably identical — verified by the exact-count
  tests at N = 16/17/18 (1 846 955 / 11 977 939 / 83 263 591). Measured wall-clock on a
  multi-core dev box: N = 16 446 → 271 ms (1.65×), N = 17 3058 → 2152 ms (1.42×),
  N = 18 20 983 → 13 725 ms (1.53×). Addresses the "CPU utilisation drop at N = 19
  Unique CountOnly" tail-imbalance investigation in `docs/ROADMAP.md`.

### Docs
- **`README.md`** — replaced the single-line placeholder with a full README covering:
  features, algorithm overview, project structure, prerequisites, build & run instructions
  (Console interactive + non-interactive flag reference, WPF GUI), solver options table,
  known solution counts (OEIS A000170 / A002562), benchmark results, contributing guide,
  and licence.
- **`docs/ROADMAP.md`** — new persistent roadmap document. Records current project
  state (release, branch, test count, coverage snapshot), the active kernel
  test-coverage track (per `BitmaskSolver.*.cs` partial), and the consolidated
  backlog (kernel performance items from `Code Analysis - 02-02.2026.txt` and
  `Potential All Mode Improvements.txt`, four known GUI issues, and the N = 15
  `n % 6 == 3` constructive-placement defect surfaced by
  `BitmaskSolverSingleModeTests`). Includes a workflow rule that ties roadmap
  updates to `CHANGELOG.md` entries so the two documents stay in sync.
- **`.github/copilot-instructions.md`** — added a `### Roadmap` section pointing
  every new Copilot session at `docs/ROADMAP.md` so the roadmap is auto-loaded as
  context. Also updated the partial-file list under `### Solver Conventions` to
  include the two newly extracted partials (`BitmaskSolver.CountUnique.cs`,
  `BitmaskSolver.Materialize.cs`).

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
  Measured results on Intel i7-14700K / .NET 10.0.8 (5 iterations, 2 warmup):
  - Unique N=12: **−12%** (836 µs vs 955 µs)
  - Unique N=14: **−7%** (12.9 ms vs 13.8 ms)
  - Unique N=16: flat (220 ms vs 226 ms, +2.4% within error — both configs take the
    same `CountUniqueFastHalfBoard` path; `CountUniqueAdaptive` forces pruning flags on
    regardless of input settings, so `UseAdaptiveDepth` and external flag values are
    irrelevant here; observed spread is thermal/TurboBoost variance)
  - All N=12: flat (355 µs vs 360 µs)
  - All N=14: flat (4.66 ms vs 4.72 ms)
  - All N=16: flat (197 ms vs 188 ms, within noise)
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
