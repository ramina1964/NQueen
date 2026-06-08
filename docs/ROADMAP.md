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

> 🎯 **This session's experiment — Profile-first investigation of `CountCanonicalDFS`.**
> The branch was originally opened to port `CountCanonicalDFS` to an iterative DFS, on the
> theory that the bottom-heavy recursion profile (~97 % of self-CPU in the loop body,
> deepest two frames carrying ~69 % of self-time) implied real call/ret overhead. Reviewing
> the archive in `docs/ignored/` before writing any code surfaced a counter-signal that
> downgraded the experiment from "high confidence" to "needs evidence":
>
> > _The most recent perf round (`feature/kernel-perf-small-wins`) explicitly tried to
> > reduce per-recursion work — saving/restoring ref state around the recursive call to
> > eliminate per-branch flag copies — and produced **no measurable speedup**. That
> > finding is closely adjacent to "remove call/ret overhead", and it strongly suggests
> > the recursion is **not** the dominant cost the profile shape would naïvely imply._
>
> What the archive **does not** contain: any record of an iterative-core port of
> `CountCanonicalDFS` itself (the "iterative" mentions in
> `docs/ignored/Archive/Potential All Mode Improvements.txt` refer to the earlier,
> superseded `CanonicalUniqueSearchEngine` and to the still-pending **All-mode** port,
> not to the current Unique count path). So the experiment is novel, but the evidence
> for the win is weaker than first claimed.
>
> **Pivot — Option C (profile-first), no production-code changes this session.** Capture
> a fresh line-level / instruction-level CPU trace on `CountCanonicalDFS` before deciding
> whether to ship an iterative port. The trace either confirms call/ret + register
> save/restore is a measurable share of self-CPU (→ proceed with the port on its own
> branch with high confidence), or refutes it (→ close `perf/unique-iterative-core`
> without code changes, record the negative finding here, pick a different candidate).
>
> **Plan of work (in order):**
> 1. **MEASURE first** — run `UniqueFastHalfBoardEvenOddBenchmark` on this freshly-merged
>    `main` to re-establish the baseline. Target to verify or update: **N=16 ≈ 244 ms,
>    N=17 ≈ 2,042 ms** (±1 % on the dev machine).
> 2. **Capture a line-level / instruction-level CPU profile** of `CountCanonicalDFS`
>    (e.g. VS Performance Profiler "CPU Usage" with line-level attribution + the
>    Hot Path view, or `dotnet-trace` with line-resolution sampling). The question to
>    answer: of the ~97 % self-CPU in the loop body, what fraction is **(a)** the bit-scan
>    + arithmetic + diagonal shifts at line 181-185 / 201-203, vs **(b)** the
>    function-prologue / call / ret / epilogue, vs **(c)** the prune-guard branch at
>    line 194-199, vs **(d)** the cancellation-throttle branch at line 171-172?
> 3. **Decide based on (b)** — if call/ret + prologue/epilogue is, say, **≥ 8 %** of
>    self-CPU, the iterative port is worth a separate `perf/unique-iterative-dfs` branch;
>    if it's a few percent or less, close this branch with the negative finding recorded
>    here and pivot to the strongest remaining candidate (most likely **work-stealing**
>    for All mode, given the documented tail-imbalance evidence in
>    `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20`).
> 4. **No production-code changes on this branch.** The branch carries only docs +
>    profiling artefacts. The eventual implementation, if any, ships on its own branch.
>
> **Out of scope for this branch:** writing the iterative port, the All-mode iterative
> core port, MRV ordering, `ArrayPool<T>` for stacks, and the cached-mask experiment.
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
  machine: **N=16 ≈ 244 ms, N=17 ≈ 2,042 ms** (error bars ≈ ±1 %).
- Profiler finding: ~97 % of self-CPU is the `CountCanonicalDFS` loop body itself
  (bit-scan + recursion + diagonal shifts), not the prune gate. The recursion profile is
  bottom-heavy (deepest two frames carry ~69 % of self-time).

**Candidate queue (pick ONE per branch, MEASURE first):**

1. **Iterative core for the Unique hot path** — _**UNDER INVESTIGATION this session**_ on
   `perf/unique-iterative-core` (Option C: profile-first, no production-code changes).
   The bottom-heavy recursion shape suggests per-frame call/ret overhead, but the
   `feature/kernel-perf-small-wins` round already reduced per-recursion work and saw no
   measurable speedup, so the win is not yet evidence-backed. A line-level CPU trace
   decides whether the port is worth a separate implementation branch. (Largest change
   if it ships.)
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
| Active branch | `perf/unique-iterative-core` (deferred-perf candidate #1, **profile-first investigation** rather than implementation: archive review surfaced a counter-signal — the prior `feature/kernel-perf-small-wins` round reduced per-recursion work and got no measurable speedup, so a line-level CPU trace decides whether the iterative port is worth a separate implementation branch). `main` at `f75c5ea` (PR #13 `refactor/event-migration` event→push-sink migration squash-merged). |
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
