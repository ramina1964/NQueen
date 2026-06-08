# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Next session — start here

> Updated after merging the `refactor/event-migration` PR into `main` (2026-06-08, squash `f75c5ea`).

**Active branch — `perf/unique-iterative-core`.** Off freshly-merged `main` (`f75c5ea`). The
event-migration track is shipped (Stages 0–6 + Stage 4 follow-up + the
`EnumerateVisualizeAdaptive` consolidation + the post-migration docs sweep) — see
`CHANGELOG.md` `[Unreleased]`. The *deferred perf track* below is now active: pick **one**
candidate per branch, MEASURE first, A/B against the frozen baseline.

> 🎯 **This session's experiment — Iterative core for the Unique hot path.**
> Picked because the profiler captured on `feature/kernel-perf-small-wins` shows ~97 % of
> self-CPU in `CountCanonicalDFS` with a **bottom-heavy** recursion profile (deepest two
> frames = ~69 % of self-time). The bottom-heavy shape strongly suggests per-frame
> call/return + register-spill overhead is a real cost; an allocation-free iterative DFS
> (the same approach already used in `BitboardNQueenSolver`) targets that overhead head-on.
> The other two candidates (cached shifted diagonal masks, depth-based work-stealing) are
> deferred — masks because the ROADMAP itself flags them as *skeptical* without a
> line-level CPU trace, and work-stealing because it only helps the >8-core tail-imbalance
> case rather than the dominant loop body.
>
> **Plan of work (in order):**
> 1. **MEASURE first** — run `UniqueFastHalfBoardEvenOddBenchmark` on this freshly-merged
>    `main` to re-establish the baseline. Target to beat: **N=16 ≈ 244 ms, N=17 ≈ 2,042 ms**
>    (±1 % on the dev machine).
> 2. **Port `CountCanonicalDFS` to an iterative core** — replace recursion with an
>    explicit per-depth state stack (column / diagonal masks + remaining-bits register).
>    Allocation-free: stack lives on a fixed-size `Span<>` sized to N. Preserve every
>    existing prune (reflection-only `ShouldPrunePrefixFull`, the depth-2 work-item
>    parallelisation, the half-board restriction, the `(col & 0xF) == 0` cancellation
>    throttle).
> 3. **Verify correctness** — full test suite (513 / 513 expected) plus the OEIS-pinned
>    counts in `BitmaskSolverUniqueTests` for N=4…16. Any deviation aborts the experiment.
> 4. **A/B benchmark** — run `UniqueFastHalfBoardEvenOddBenchmark` again; report
>    wall-clock delta and confidence interval. Target: a **measurable** speedup outside
>    the ±1 % noise band on both N=16 and N=17.
> 5. **Decide** — ship if it wins; revert (and record the negative finding here) if it
>    doesn't, the same way `feature/kernel-perf-small-wins` was honestly recorded as
>    "correctness-neutral, no measurable speedup".
>
> **Out of scope for this branch:** the All-mode iterative core port (separate experiment
> if Unique wins clearly), MRV ordering, `ArrayPool<T>` for stacks, and the cached-mask
> experiment. Each gets its own `perf/<specific-name>` branch per the rule below.

**Deferred perf track — context.** The notes below are the authoritative profiling
record from `feature/kernel-perf-small-wins`. They guide candidate selection across
multiple future perf branches.

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

**Candidate queue (pick ONE per branch, MEASURE first):**

1. **Iterative core for the Unique hot path** — _**ACTIVE this session**_ on
   `perf/unique-iterative-core`. The bottom-heavy recursion hints call/return overhead is
   significant; port the allocation-free iterative DFS. (Largest change.)
2. **Cached shifted diagonal masks** — remove repeated `(d1|bit)<<1` / `(d2|bit)>>1` in the
   hottest loop. *Skeptical:* the shifts depend on a per-iteration `bit`, so capture a
   line-level CPU trace to confirm there is real redundancy before committing.
3. **Depth-based work-stealing queue** for All mode at large N (tail-imbalance on >8 cores).

**Process (rule).** Branch off freshly-merged `main` with a name tied to the *specific*
experiment (e.g. `perf/unique-iterative-core`, not a generic "small-wins" name) so the
branch can't over-promise. Run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish the
baseline before touching production code, per the team's MEASURE-first practice.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `perf/unique-iterative-core` (deferred-perf candidate #1: port `CountCanonicalDFS` to an allocation-free iterative DFS; targeting the bottom-heavy recursion overhead from the `feature/kernel-perf-small-wins` profiling round). `main` at `f75c5ea` (PR #13 `refactor/event-migration` event→push-sink migration squash-merged). |
| Target framework | .NET 10 across all projects (`net10.0` / `net10.0-windows` for GUI) |
| Test count | **489 / 489 passing** (400 unit + 89 view-model). Down from 513 pre-Stage-6 because Stage 6 deleted one obsolete `ShouldIgnorePreSetCancellationFlag` test and consolidated the cancellation tests onto `CancellationTokenSource`s; net coverage of the cancellation surface is unchanged or improved. |
| Code coverage | Stale (last full run 2026-05-29: Domain 93 %, Kernel 67 %, Shared 95 %, Total 77 %). Re-collect pending. |
| Build status | 0 errors / 0 warnings |

### Recently shipped (see `CHANGELOG.md` `[Unreleased]` for full detail)

- Event migration (`refactor/event-migration`, PR #13, squash `f75c5ea`): replaced the
  `BitmaskSolver` `event` surface (`QueenPlaced` / `SolutionFound` / `ProgressValueChanged` +
  `SetSimulationToken` + `IsSolverCanceled`) with per-call push sinks on `SimulationContext` —
  `IProgress<ProgressInfo>` (progress), `IProgress<SolutionFoundInfo>` (solutions, synchronous
  via `SynchronousProgress<T>`), `ChannelWriter<QueenPlacedInfo>` (animation, conflating
  bounded channel with `DropOldest`), and a real `CancellationToken`. Stages 0–6 + the §1a
  pre-work leak audit (preventive finding, no live leak) + the Stage 4 follow-up Visualize
  bug fixes (build-up-halt, All-mode-no-animate, list-selection-wrong-board) + the
  `EnumerateAllVisualizeAdaptive`/`EnumerateUniqueVisualizeAdaptive` consolidation into a
  single `EnumerateVisualizeAdaptive(bool isUnique)` + the post-migration docs sweep all
  ship in this PR. Net diff +900 / -447 across 44 files.
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

- _(none currently — the Unique-mode count throttle previously listed here was for
  the deleted `IsSolverCanceled` field; the equivalent throttle on the cancellation-token
  poll is already in place: the `(col & 0xF) == 0` gate on `IsCancellationRequested` in
  `BitmaskSolver.CountUnique.cs::CountCanonicalDFS` keeps the read off the hottest path.)_

### Larger wins, scoped risk

> _Three of the items below — **Iterative core for the Unique hot path**, **Cached shifted
> diagonal masks**, and **Depth-based work-stealing queue (All mode)** — are also tracked
> in the **Candidate queue** under *Next session — start here* with selection rationale,
> a "MEASURE first" baseline, and an "ACTIVE this session" marker for whichever is currently
> being experimented on. Treat the *Next session* block as the live picker and this list as
> the wider perf inventory._

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
