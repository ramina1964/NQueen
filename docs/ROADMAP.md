# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Next session — start here

> Updated after opening `perf/all-work-stealing` off `main` (`f75c5ea`).

**Active branch — `perf/all-work-stealing`.** Off `main` (`f75c5ea`). The
`perf/unique-iterative-core` negative finding is on `main`; the deferred-perf
track continues with Candidate queue #3 — MEASURE first, A/B against the frozen
baseline.

> 🎯 **This session's experiment — Depth-based work-stealing queue for All mode.**
> Evidence: `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20` documents
> tail-imbalance at large N on >8 cores with the current root-only `Parallel.ForEach`
> scheduling. The hypothesis is that replacing root-only scheduling with a
> `ConcurrentQueue<PartialState>` consumed by worker threads will reduce tail imbalance
> and improve wall-clock at large N (e.g. N ≥ 16) on the dev machine (i7-14700K, 28
> logical / 20 physical cores).
>
> **Plan of work (in order):**
> 1. **MEASURE first** — re-run `UniqueFastHalfBoardEvenOddBenchmark` (full job: 3 warmups,
>    15 iterations, N=16 even + N=17 odd) on freshly-checked-out `perf/all-work-stealing`
>    to re-establish the baseline before touching any production code. Authoritative
>    reference: **N=16 ≈ 254.8 ms (±1.25 %), N=17 ≈ 2,103.0 ms (±0.93 %)** from
>    `main` `f75c5ea` (2026-06-08).
> 2. **Design the work-stealing structure** — decide queue granularity (depth-2 partial
>    states vs. deeper), work-item representation (`PartialState` struct carrying
>    cols/d1/d2 masks + current depth), and thread-count strategy (fixed pool vs.
>    `Environment.ProcessorCount`).
> 3. **Implement on All-mode path only** — modify `BitmaskSolver.All.cs`; leave Unique
>    path untouched.
> 4. **A/B benchmark** — run `UniqueFastHalfBoardEvenOddBenchmark` (and a new All-mode
>    equivalent benchmark if one does not exist) with the same full-job settings to
>    measure the delta.
> 5. **Decide** — if wall-clock improvement exceeds the ±1 % noise band at N ≥ 16, ship;
>    otherwise record the negative finding and close the branch docs-only.
>
> **Out of scope for this branch:** Unique-mode work-stealing, MRV ordering,
> `ArrayPool<T>` for stacks, cached diagonal masks, and iterative All-mode core.
> Each gets its own `perf/<specific-name>` branch per the rule below.

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
  machine (i7-14700K, .NET 10.0.8, freshly-merged `main` `f75c5ea`, captured 2026-06-08):
  **N=16 ≈ 254.8 ms (±1.25 %), N=17 ≈ 2,103.0 ms (±0.93 %)**. About 3–4 % slower than the
  pre-event-migration baseline (244 / 2,042 ms) — well within natural cross-build drift,
  but the numbers above are the new authoritative reference for any A/B done on this
  `main`.
- Profiler finding: ~97 % of self-CPU is the `CountCanonicalDFS` loop body itself
  (bit-scan + recursion + diagonal shifts), not the prune gate. The recursion profile is
  bottom-heavy (deepest two frames carry ~69 % of self-time).

**Candidate queue (pick ONE per branch, MEASURE first):**

1. ~~**Iterative core for the Unique hot path**~~ — _**ABANDONED (2026-06-08)**_ on
   `perf/unique-iterative-core` after a profile-first investigation closed it as a
   negative finding. Line-level attribution via `profile_unit_test` showed no
   call/ret/prologue/epilogue bucket as a separate sample group; the only non-trivial
   leaf, `BitOperations.TrailingZeroCount` at 5.01 % Self, was tested with a
   `Bmi1.X64.TrailingZeroCount` intrinsic pre-experiment and produced no wall-clock
   movement (+0.7 % at N = 16, fully inside the ±1.11 % noise band) even though the
   leaf physically vanished from the post-edit profile. The recursion shape's
   bottom-heaviness reflects real bit-scan + arithmetic + diagonal-shift work in the
   deepest frames, not call-frame overhead the iterative port could remove. Skip on
   future branches.
2. **Cached shifted diagonal masks** — remove repeated `(d1|bit)<<1` / `(d2|bit)>>1` in the
   hottest loop. *Skeptical:* the shifts depend on a per-iteration `bit`, so capture a
   line-level CPU trace to confirm there is real redundancy before committing.
3. **Depth-based work-stealing queue** for All mode at large N (tail-imbalance on >8 cores)
   — **🎯 active on `perf/all-work-stealing`**.

**Process (rule).** Branch off freshly-merged `main` with a name tied to the *specific*
experiment (e.g. `perf/all-work-stealing`, not a generic "small-wins" name) so the
branch can't over-promise. Run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish the
baseline before touching production code, per the team's MEASURE-first practice.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `perf/all-work-stealing` — Candidate queue #3: depth-based work-stealing queue for All mode at large N (tail-imbalance on >8 cores). Branch opened off `main` (`f75c5ea`). Step 1 pending: re-run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish baseline before touching production code. |
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
