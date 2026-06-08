# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Next session — start here

> Updated after merging the `refactor/gui` PR into `main` (2026-06-07).

> 📌 **Design docs awaiting execution (read if picking up that track):**
> - [`docs/EVENT-MIGRATION-PLAN.md`](EVENT-MIGRATION-PLAN.md) — staged plan to replace the
>   solver's `event` surface (`QueenPlaced` / `SolutionFound` / `ProgressValueChanged` +
>   `SetSimulationToken` + `IsSolverCanceled`) with per-call push sinks (`IProgress<T>` +
>   conflating `Channel<T>` + `CancellationToken`). **In progress on branch
>   `refactor/event-migration`.** **Stage 0 — DONE:** the behaviour-preserving notification-seam
>   extraction shipped (all ~25 raise sites collapsed behind `RaiseProgress` / `RaiseQueenPlaced` /
>   `RaiseSolutionFound`; the ungated terminal-100% progress quirk fixed so the `EnableEvents` gate
>   is uniform). **Stage 1 — DONE:** progress migrated to `IProgress<ProgressInfo>` (new
>   `ProgressInfo` record + `SimulationContext.OnProgress` sink); the `ProgressValueChanged` event,
>   `SetSimulationToken`, the `Guid` simulation token (VM + solver + engine `Request`), and
>   `ProgressUpdateEventArgs` are all deleted; the VM builds a per-run `Progress<ProgressInfo>`.
>   **Stage 2 — DONE:** a real `CancellationToken` is threaded VM → `SimulationContext.Cancellation`
>   → solver hot loops via one internal `IsCancellationRequested` (OR of the legacy bool + token);
>   `IsSolverCanceled` stays as a thin shim until Stage 5. **Stage 3 — DONE:** `SolutionFound`
>   migrated to a synchronous `IProgress<SolutionFoundInfo>` sink (new `SolutionFoundInfo` record +
>   `SimulationContext.OnSolutionFound` + a `SynchronousProgress<T>` adapter that preserves the
>   solver-thread buffer-copy semantics); `RaiseSolutionFound` dual-emits (event + sink) so the
>   kernel event tests stay green until the event is deleted in Stage 5. **Stage 4 — DONE:**
>   `QueenPlaced` migrated to a conflating, keep-latest `Channel<QueenPlacedInfo>` (new
>   `QueenPlacedInfo` record + `SimulationContext.OnQueenPlaced` `ChannelWriter`) drained by the
>   existing visualization `DispatcherTimer`; `RaiseQueenPlaced` dual-emits (event +
>   `TryWrite` of a copied prefix), the channel is wired only in Visualize mode, and both animation
>   paths are preserved (one column per tick with a delay, full latest prefix at zero delay).
>   **Stage 5 — DONE:** the event scaffolding is removed — the `QueenPlaced` / `SolutionFound`
>   events drop off `ISolverFrontEnd` and `BitmaskSolver` (the `Raise*` helpers are now sink-only),
>   the `QueenPlacedEventArgs` / `SolutionFoundEventArgs` types and their dead global usings are
>   deleted, and the vestigial `Subscribe/UnsubscribeFromSimulationEvents` plumbing (no-op after
>   Stage 4) is removed from the view-model. Six kernel/view-model tests are rewritten onto the
>   sinks via shared `SynchronousProgress<T>` / `CallbackChannelWriter<T>` test helpers.
>   `EnableEvents` (now a sink master-switch) and the `IsSolverCanceled` bool are intentionally
>   retained. **Stage 4 follow-up fixes — DONE:** smoke testing surfaced three Visualize
>   regressions. (1) The build-up animation stopped after the first solution — the
>   `SelectedSolution` setter's `StopVisualizationTimer()` tore down the channel-drain timer that
>   Stage 4 introduced; the setter now leaves the board to the timer during a live Visualize run, and
>   `SimulationCompleted` is raised after the final board render. (2) All-mode Visualize never
>   animated (all solutions appeared at once) because `RunAllUnified` hardcodes `Hide` + a no-op
>   `OnQueenPlaced`; added `EnumerateAllVisualizeAdaptive()` (two-phase: animated Phase 1 capped at
>   `MaxDisplayedCount`, then a fast silent half-board count) and routed All+Visualize to it in
>   `HandleModeCommon`. (3) All-mode list selection rendered the wrong board for every non-first
>   entry — the three All-mode materialize sites stored each board's *canonical* key, collapsing
>   distinct boards onto one placement; they now store the actual board via `SymmetryHelper.PackRows`
>   (inverse of `Solution.Unpack`). **Stage 6 — DONE:** the `IsSolverCanceled` bool is collapsed
>   onto the `CancellationToken` — `ISolverBackEnd` / `BitmaskSolver` / GUI VM
>   (`Commands.cs` / `Events.cs` / `Progress.cs`) / Console / Benchmarks all migrated to read
>   token-based cancellation, and five cancellation tests across `BitmaskSolverSingleModeTests` /
>   `…UniqueTests` / `…AllModeTests` / `…ModeTests` / `SolverTests` are rewritten onto local
>   `CancellationTokenSource`s (the last one renamed to
>   `BitmaskSolver_SingleMode_HonorsPreCancelledToken_ReturnsWithoutThrowing` and switched from
>   `Solve()` to `await GetSimResultsAsync(ctx)` so the token actually reaches the kernel). Build
>   0/0; fast suite **489 / 489** (400 unit + 89 view-model). **Next:** post-migration docs sweep —
>   `README.md` Solver-Options table + `.github/copilot-instructions.md` event-args note (small,
>   separate commit on its own branch). See `CHANGELOG.md` `[Unreleased]`.
> - **§1a pre-work audit — DONE (finding: _preventive_, not corrective).** No live
>   lapsed-listener leak exists. A workspace-wide search found exactly six subscription sites
>   (all named-method `+=`/`-=` pairs in `MainViewModel.Events.cs`, zero lambdas), backed by an
>   idempotent subscribe, a per-run subscribe/unsubscribe cycle, a dispose chain, and a DI
>   lifetime graph where the solver never outlives the VM. The migration is therefore
>   *preventive* — it makes the no-leak guarantee **structural** (regression-proof against future
>   per-run / multi-window lifetime changes), not a fix for a live defect. See `CHANGELOG.md`
>   `[Unreleased]` for the full evidence.

**Most recent session — GUI refactor (`refactor/gui`), now MERGED (PR #10, squash `8f41b7a`).**
The WPF front-end was reworked: `MainWindow` is wrapped in a `Viewbox` for a user-resizable,
uniformly-scaling window (the chessboard stays square), the monitor-fit `user32` P/Invoke was
deleted (code-behind 227 → ~107 lines), Per-Monitor V2 DPI awareness was added via a new
`app.manifest`, a 4px-grid spacing-token system landed in `AppStyles.xaml`, and the
solution-list "height jump" and Simulate "phantom resize" glitches were fixed. A follow-up pass
narrowed the right control column (400 → 300) and switched panel value columns to `Auto`, then
an appearance-neutral cleanup tokenised every literal colour/caption `FontSize` and purged dead
code (5 unused types, the `Messaging/` + `MessagePruning/` folders, the dead `App.xaml`
converter resource, the `PanelStackGap` token). The kernel hot path was untouched, so the perf
baseline and findings below are unchanged. **This track is complete; the next experiment is the
deferred perf track below.**

**Deferred perf track — still the recommended next experiment.** The notes below pre-date
the GUI session. Pick ONE candidate, A/B against the frozen baseline, MEASURE first.

**Reality check on the last perf branch.** `feature/kernel-perf-small-wins` shipped the
Item 2 prune-gate tightening (hoisting `reflectionEnabled` ahead of `ShouldPrunePrefixFull`
in `CountCanonicalDFS`). It is **correctness-neutral and produced no measurable speedup** —
the change is behaviour-identical on the reflection-on path that production and the
benchmarks use. Do **not** read that branch as a performance gain; its value is the baseline
and profiling knowledge below, not faster code.

**What it left behind (use this):**

- A frozen, low-variance baseline benchmark — `UniqueFastHalfBoardEvenOddBenchmark`
  (full job: 3 warmups, 15 iterations, N=16 even + N=17 odd). Current baseline on the dev
  machine: **N=16 ≈ 244 ms, N=17 ≈ 2,042 ms** (error bars ≈ ±1 %).
- Profiler finding: ~97 % of self-CPU is the `CountCanonicalDFS` loop body itself
  (bit-scan + recursion + diagonal shifts), not the prune gate. The recursion profile is
  bottom-heavy (deepest two frames carry ~69 % of self-time).

**Where the real wins likely are — pick ONE, A/B against the baseline above, MEASURE first:**

1. **Iterative core for the Unique hot path** — the bottom-heavy recursion hints call/return
   overhead is significant; port the allocation-free iterative DFS. (Largest change.)
2. **Cached shifted diagonal masks** — remove repeated `(d1|bit)<<1` / `(d2|bit)>>1` in the
   hottest loop. *Skeptical:* the shifts depend on a per-iteration `bit`, so capture a
   line-level CPU trace to confirm there is real redundancy before committing.
3. **Depth-based work-stealing queue** for All mode at large N (tail-imbalance on >8 cores).

**Process:** branch off the freshly-merged `main` with a name tied to the *specific*
experiment (e.g. `perf/unique-iterative-core`), not another generic "small-wins" name, so the
branch can't over-promise. Run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish the
baseline before touching production code, per the team's MEASURE-first practice.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `refactor/event-migration` (event→push-sink migration; §1a leak audit done — _preventive_; **Stage 0 seam extraction + Stage 1 progress-sink / Guid-token deletion + Stage 2 CancellationToken threading + Stage 3 SolutionFound sink + Stage 4 QueenPlaced conflating-channel + Stage 5 event-scaffolding removal + Stage 6 IsSolverCanceled→CancellationToken collapse shipped**). `main` at `42d2530` (PRs #11 test consolidation, #12 ROADMAP sync — both merged). |
| Target framework | .NET 10 across all projects (`net10.0` / `net10.0-windows` for GUI) |
| Test count | **513 / 513 passing** (424 unit + 89 view-model; up from 304 at v1.0.0). The Fact→Theory consolidation (PR #11) reduced *method* count but kept every scenario as a visible `[InlineData]` case — net +2 vs the prior 511 because two Facts that looped internally over `{2, 3}` now report each input as its own case. |
| Code coverage | Stale (last full run 2026-05-29: Domain 93 %, Kernel 67 %, Shared 95 %, Total 77 %). Re-collect pending. |
| Build status | 0 errors / 0 warnings |

### Recently shipped (see `CHANGELOG.md` `[Unreleased]` for full detail)

- GUI cleanup (`refactor/gui`): appearance-neutral XAML magic-constant purge — every literal
  colour and caption `FontSize` across the seven view XAMLs routed through new `AppStyles.xaml`
  brushes (`SurfaceBrush`, `TextPrimaryBrush`, `TextMutedBrush`, `TextSubtleBrush`,
  `SelectionForegroundBrush`, error trio) and a `CaptionFontSize` token; plus a dead-code purge
  (5 unused converter/utility types, the build-excluded `Messaging/` + `MessagePruning/` folders,
  the dead `App.xaml` converter resource, and the unused `PanelStackGap` token).
- GUI refactor (`refactor/gui`): `MainWindow` wrapped in a `Viewbox` for a user-resizable,
  uniformly-scaling window (chessboard stays square); monitor-fit `user32` P/Invoke removed
  (code-behind 227 → ~107 lines); Per-Monitor V2 DPI awareness added via new `app.manifest`;
  4px-grid spacing-token system in `AppStyles.xaml`; solution-list "height jump" and Simulate
  "phantom resize" fixes; control column narrowed 400 → 300 with panel value columns switched
  to `Auto`. (Merged to `main` via PR #10, squash `8f41b7a`.)
- Kernel performance: `TZCNT` intrinsic, `SearchState` struct, `EnsureMinThreads` one-shot guard.
- Kernel performance: depth-2 work-item parallelisation of `CountUniqueFastHalfBoard`
  (~180 fine-grained (col-0, col-1) items at N = 20 vs ~10 coarse root-row ranges),
  giving 1.4–1.65× wall-clock on N = 16–18 with provably identical counts.
- Kernel correctness: two-phase `EnumerateUniqueVisualizeAdaptive` (~2× fewer nodes for Unique Visualize).
- Kernel correctness: fixed a Unique-mode count under-report at N >= 16
  (`CountUniqueFastHalfBoard` returned 692 857 instead of 1 846 955 at N = 16); the
  shared forward-prefix prune `ShouldPrunePrefixFull` is now reflection-only after
  removing an unsound rotate-180 minimality test.
- Kernel correctness: fixed `GenerateConstructiveSolution` emitting invalid placements
  for `n % 6 ∈ {2, 3}` (e.g. N = 15, 20 placed two queens on a shared anti-diagonal);
  rewritten to the canonical `n mod 6` closed-form construction.
- Kernel refactor: `BitmaskSolver.cs` further split into `BitmaskSolver.CountUnique.cs` and `BitmaskSolver.Materialize.cs` (710 → 268 lines in the root file).
- Test coverage: dedicated tests for `BitmaskSolver.CountUnique.cs` (14 tests),
  `BitmaskSolver.Single.cs` (17 tests), `BitmaskSolver.All.cs` (17 tests —
  12 declarations + 5 Theory expansions), `BitmaskSolver.Unique.cs`
  (15 tests — 11 declarations + 4 Theory expansions), and
  `BitmaskSolver.Materialize.cs` (7 tests — 5 declarations + 2 Theory expansions).
- GUI: `MainWindow.xaml` clipping fix; `ListOfSolutionsUserControl.xaml` height cap.
- Console: solver config aligned with GUI (pruning on, adaptive depth, half-board for All ≥ 15).
- Benchmarks: 14 files consolidated to 7; new `ConsolePruningImpactBenchmarks`.
- Docs: `README.md` rewritten from a placeholder into a full project overview.

---

## Active Track — Kernel Test Coverage

Goal: bring every `BitmaskSolver.*.cs` partial up to a dedicated test class so future
refactors don't silently regress branch coverage. Each entry below is one PR-sized
unit of work.

| Partial file | Dedicated test class | Status | Baseline line / branch coverage |
|---|---|---|---|
| `BitmaskSolver.cs` (root) | covered indirectly by existing suite | n/a | — |
| `BitmaskSolver.Single.cs` | `BitmaskSolverSingleModeTests` (17 tests) | **shipped** | was 41 % / 25 % |
| `BitmaskSolver.CountUnique.cs` | `BitmaskSolverCountUniqueTests` (14 tests) | **shipped** | was 15 % / 3 % |
| `BitmaskSolver.All.cs` | `BitmaskSolverAllModeTests` (17 tests) | **shipped** | TBD — pending re-collect |
| `BitmaskSolver.Unique.cs` | `BitmaskSolverUniqueTests` (15 tests) | **shipped** | TBD — pending re-collect |
| `BitmaskSolver.Materialize.cs` | `BitmaskSolverMaterializeTests` (7 tests) | **shipped** | TBD — pending re-collect |

Every `BitmaskSolver.*.cs` partial now has a dedicated test class — the track is
complete. Next: regenerate the coverage report and fill in the baseline column with
the new numbers.

---

## Backlog — Kernel Performance

Consolidated from `docs/ignored/Archive/Code Analysis - 02-02.2026.txt` and
`docs/ignored/Archive/Potential All Mode Improvements.txt`. Items are listed roughly by
effort × expected impact.

### Small wins, low risk

- **Throttle `IsSolverCanceled` reads** in the `CountCanonicalDFS` hot loop
  (don't check on every `while` iteration). Source: `Code Analysis - 02-02.2026.txt`.
  Partly addressed — the check is now gated to `(col & 0xF) == 0` (once per 16 columns).

### Larger wins, scoped risk

- **Depth-based work-stealing queue** for All mode at large N — replace root-only
  scheduling with a `ConcurrentQueue<PartialState>` consumed by worker threads to
  reduce tail imbalance on >8 cores. Source: `Potential All Mode Improvements.txt`.
- **Symmetry reduction in All count-only path** beyond the existing half-board
  restriction (enumerate fundamental representatives and expand by symmetry factor).
- **`ArrayPool<T>`** for column / diagonal / row stacks to reduce GC pressure at N ≥ 18.
- **Iterative core for All mode** — port Unique's allocation-free iterative DFS.
- **MRV heuristic** for next-column branch ordering (cost / benefit needs benchmarking).
- **Cached shifted diagonal masks** per row to remove repeated `(d1|bit)<<1` /
  `(d2|bit)>>1` in the hottest loop.

### Investigations

- **Unique CountOnly vs Materialize gap** at N = 17–19 — historical data shows a
  ~5–6× difference. Two-phase split in `EnumerateUniqueVisualizeAdaptive` closed
  part of the gap but there is likely more to find.

---

## How to keep this document honest

When a tracked item ships, in the **same commit**:

1. Move the item out of this file (delete the row / bullet, or flip "in progress" → remove).
2. Add a corresponding entry to `CHANGELOG.md` under `[Unreleased]`.
3. If the change shifts the "Current State" table (test count, coverage, branch name,
   build status), update those numbers here.
