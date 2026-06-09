# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Next session — start here

> Updated after `perf/all-mode-symmetry-reduction` closed as the **fourth profile-first
> negative finding in a row** (see *Recently shipped* below). The previous active branch
> `docs/roadmap-sync-post-pr17` merged as **PR #18** (post-PR-#17 docs sync; docs-only).
> The execution queue continues to *next sub-item* under `Backlog → Larger wins, scoped
> risk`.

**Active branch — `perf/all-mode-symmetry-reduction`.** Docs-only branch, complete
pending merge. Opened off freshly-merged `main` (post-PR #18, `7d07a69`) with the
explicit scope of *deciding* whether the remaining D4 factor of up to 4× — beyond the
existing half-board reflection — could be extracted on the All count-only path via a port
of the reflection-only forward-prefix prune `SearchHelpers.ShouldPrunePrefixFull` from
the Unique path ("Experiment A1"). Branch baseline
`branch-baseline-all-mode-symmetry-reduction.md` (cross-checked across two existing
harnesses on `7d07a69`): `AllCountOnlyN18Benchmark` N = 18 ≈ 7,403 ms ±0.67 %, and
`AllCountOnlyParallelScalingBenchmark` N = 16 ≈ 148.1 ms ±0.78 %, N = 18 ≈ 7,358.8 ms
±0.77 %. **The kill signal arrived from code reading before any production change**:
for any `row0 ∈ [0, N/2)` on **even N**, `row0 < N-1-row0` strictly, so
`ShouldPrunePrefixFull`'s loop hits `break` at `i = 0` and returns `false` for every
node — the prune would fire **zero times** at N = 16 / N = 18, guaranteeing pure overhead
for zero pruning benefit. The structural argument closes the entire candidate, not just
Experiment A1: the half-board restriction already captures the maximal subgroup of the
row-reflection prune on even N; the remaining D4 factor lives in the rotations (rot90,
rot180, rot270), which are not column-preserving and require leaf-level canonical-form
checking — pure overhead on a path at 99.99 % Self CPU on register-tight bit-mask
operations. Branch ships docs-only.

**Execution queue — progress.** Step 1 (Documentation drift housekeeping) shipped as
PR #18. Step 2.1 (Symmetry reduction in All count-only path) closes here as the fourth
profile-first negative finding. Next up: step 2.2 — **Iterative core for All mode**
(port Unique's allocation-free iterative DFS over to the `BitboardNQueenSolver.Search`
site). Then 2.3 (MRV heuristic), then 3 (Investigations — Unique CountOnly vs
Materialize gap at N = 17–19), then 4 (Test Coverage closeout), then 5 if 5 ever gets
populated.

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
2. ~~**Cached shifted diagonal masks**~~ — _**ABANDONED (2026-06-10)**_ on
   `perf/cached-diagonal-shifts` after a profile-first investigation closed it as the
   second negative finding in the queue (matching the queue-#1 pattern from
   `perf/unique-iterative-core`). Branch baseline re-established at
   **N = 17 ≈ 1,416.3 ms ±0.53 %** (`branch-baseline-cached-diagonal-shifts.md`);
   line-level CPU attribution via `profile_unit_test` against
   `BitmaskSolverCountUniqueTests.CountUniqueAdaptive_PreservesPruningFlags(n: 16, …)`
   confirmed the skeptical prior: the `(d1|bit)<<1` and `(d2|bit)>>1` expressions at
   `BitmaskSolver.CountUnique.cs:207` do **not** surface as a separate sample band —
   they fold into the recursive `CountCanonicalDFS` per-frame Self% (peak 14.66 % at
   col 5, 13.85 % at col 6, 12.05 % at col 7) alongside `avail ^= bit`, `rows[col] = r`,
   and the prune-gate branch. The decision gate (≥ 2–3 % Self attributable specifically
   to the shifts → write the cache; < 1 % or folded into the bit-scan loop → abandon)
   returned the negative branch. Because `bit = avail & -avail` is recomputed and unique
   per iteration, a per-iteration "cache" can only rename the two ALU ops, not amortise
   them. Skip on future branches.
3. ~~**Depth-based work-stealing queue** for All mode at large N~~ — _**SHIPPED**
   (2026-06-09)_ on `perf/all-work-stealing`. Option A (chunk-of-1 dynamic partitioner
   over the existing depth-2 work items) cleared the ±1 % band decisively: N = 16
   **-17.0 %** (151.0 ms vs 182.0 ms baseline), N = 18 **-24.1 %** (7.39 s vs 9.74 s
   baseline), CIs non-overlapping at both Ns. Option B (true work-stealing queue) was
   designed but not needed. See *Recently shipped* and `CHANGELOG.md [Unreleased] →
   Performance` for the full A/B numbers, hypothesis confirmation, and correctness gates.

**Process (rule).** Branch off freshly-merged `main` with a name tied to the *specific*
experiment (e.g. `perf/all-work-stealing`, not a generic "small-wins" name) so the
branch can't over-promise. Run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish the
baseline before touching production code, per the team's MEASURE-first practice.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `perf/all-mode-symmetry-reduction` — **docs-only, complete pending merge.** "Symmetry reduction in All count-only path" candidate (from `Backlog → Larger wins, scoped risk`) abandoned as the **fourth profile-first negative finding in a row**. Branch baseline `branch-baseline-all-mode-symmetry-reduction.md` (cross-checked on two existing harnesses on `7d07a69`): `AllCountOnlyN18Benchmark` N = 18 ≈ 7,403 ms ±0.67 %; `AllCountOnlyParallelScalingBenchmark` N = 16 ≈ 148.1 ms ±0.78 %, N = 18 ≈ 7,358.8 ms ±0.77 %. Kill signal arrived from code reading before any production change: the existing half-board restriction `row0 ∈ [0, N/2)` already exhausts the row-reflection prune on even N (the prune's loop hits `break` at `i = 0` for every node), so porting `ShouldPrunePrefixFull` would fire zero times at N = 16 / 18 — pure overhead for zero benefit. The remaining D4 factor of up to 4× lives in rotations (rot90, rot180, rot270), which are not column-preserving and require leaf-level canonical-form checking — pure overhead on a 99.99 %-Self register-tight path. Decision gate returned the negative branch via the structural argument; no production-code changes; no new measurement artifact (the existing `AllCountOnlyN18Benchmark` and `AllCountOnlyParallelScalingBenchmark` from PR #15 already serve as permanent regression guards). Full detail in `CHANGELOG.md [Unreleased] → Docs`. The previous active branch `docs/roadmap-sync-post-pr17` merged as **PR #18** (post-PR-#17 docs sync; docs-only). |
| Target framework | .NET 10 across all projects (`net10.0` / `net10.0-windows` for GUI) |
| Test count | **489 / 489 passing** (400 unit + 89 view-model). Down from 513 pre-Stage-6 because Stage 6 deleted one obsolete `ShouldIgnorePreSetCancellationFlag` test and consolidated the cancellation tests onto `CancellationTokenSource`s; net coverage of the cancellation surface is unchanged or improved. |
| Code coverage | Stale (last full run 2026-05-29: Domain 93 %, Kernel 67 %, Shared 95 %, Total 77 %). Re-collect pending. |
| Build status | 0 errors / 0 warnings |

### Recently shipped (see `CHANGELOG.md` `[Unreleased]` for full detail)

- `perf/all-mode-symmetry-reduction` — "Symmetry reduction in All count-only path"
  candidate (from `Backlog → Larger wins, scoped risk`) closed as the **fourth
  profile-first negative finding in a row** (docs-only branch, no production-code
  changes). Opened off freshly-merged `main` (post-PR #18, `7d07a69`) to *decide*
  whether the remaining D4 factor of up to 4× — beyond the existing half-board
  reflection captured by `BitboardNQueenSolver.CountSolutions` (`row0 ∈ [0, N/2)` +
  `count *= 2`) — could be extracted via a port of the reflection-only forward-prefix
  prune `SearchHelpers.ShouldPrunePrefixFull` from the Unique path ("Experiment A1").
  Branch baseline cross-checked on two existing harnesses on `7d07a69`:
  `AllCountOnlyN18Benchmark` N = 18 ≈ 7,403 ms ±0.67 %, and
  `AllCountOnlyParallelScalingBenchmark` N = 16 ≈ 148.1 ms ±0.78 %, N = 18 ≈ 7,358.8 ms
  ±0.77 % (cross-benchmark agreement at N = 18: 0.6 % apart). **The kill signal arrived
  from code reading before any production change**: `SearchHelpers.ShouldPrunePrefixFull`
  (`SearchHelpers.cs:73-84`) walks `for i in [0..depth]: if rows[i] > N-1-rows[i] return
  true; if rows[i] < N-1-rows[i] break`. The existing All count-only path already
  restricts row 0 to the top half (`BitboardNQueenSolver.cs:80`, `for (int row0 = 0;
  row0 < half; row0++)`); for any `row0 ∈ [0, N/2)` on **even N**, `row0 < N-1-row0`
  strictly — so the prune's loop hits `break` at `i = 0` and returns `false` for every
  node in the tree. The benchmark targets are N = 16 and N = 18 — both even — so the
  port would fire **zero times** at the measured sizes, adding per-node overhead for
  zero pruning benefit and guaranteeing regression. The structural argument closes the
  entire candidate, not just Experiment A1: the half-board restriction already captures
  the maximal subgroup of the row-reflection prune on even N; the remaining D4 factor
  of up to 4× lives in the rotation symmetries (rot90, rot180, rot270), which are
  **not column-preserving** and therefore not amenable to forward-prefix pruning at all
  — they require leaf-level canonical-form checking, which on a path that runs at
  99.99 % Self CPU on register-tight bit-mask operations (per the PR #17 trace evidence)
  is pure overhead. A quarter-board fundamental-domain enumeration with closed-form
  orbit weighting (Option B) would have bounded upside (≈25-40 % wall-clock at best),
  non-trivial implementation (≈200-300 lines + extensive correctness validation against
  the N ≤ 18 oracle), and ≈75 % prior probability of becoming the negative finding
  regardless given the three preceding negatives — not pursued. No production-code
  changes; no new measurement artifact (the existing `AllCountOnlyN18Benchmark` and
  `AllCountOnlyParallelScalingBenchmark` from PR #15 already serve as permanent
  regression guards for this code path). Branch ships docs-only.
- `perf/all-mode-arraypool` (PR #17, squash `b0fcb4c`) — `ArrayPool<T>` for column /
  diagonal / row stacks on the All-mode materialize path (from `Backlog → Larger wins,
  scoped risk`) closed as the **third profile-first negative finding in a row**
  (docs-only branch, no production-code changes). Opened off freshly-merged `main`
  (post-PR #16) to decide whether pooling the per-call `int[]` / `Frame[]` buffers in
  `BitmaskSearchEngine.CreateState` and the per-solution `int[]` copies in
  `BitmaskSolver.All.cs` via `ArrayPool<T>.Shared` would reduce GC pressure at N ≥ 18.
  N = 14 unit-test MEMORY trace (`profile_unit_test` against
  `AllMode_Materialize_N14_RoutesThroughTwoPhasePath`) had the top-3 allocation types
  dominated by xUnit reflection plumbing (`System.String`, `CustomAttributeType`,
  `CustomAttributeNamedParameter`) with no user-code types in the top hits; (2) running
  the existing `AllModeVariantsBenchmark.All_Sequential_Materialize` in MEMORY mode
  returned timing-only output (the class lacks `[MemoryDiagnoser]`); (3) a new
  purpose-built `AllModeMaterializeAllocationBenchmark` with `[MemoryDiagnoser]` and
  `[Params(15, 18)]` likewise emitted no allocation columns (per-op allocations below
  BenchmarkDotNet's reporting threshold), AND the supplementary CPU trace pinned
  **99.99 % Self on `BitboardNQueenSolver.Search`** — the explicitly allocation-free
  static DFS that phase 2 of `CollectAllSamplesAndCountParallel` calls. Source
  inspection confirmed `BitboardNQueenSolver.Search` takes only `ulong`/`int` arguments
  with zero `new` operators in the recursion (the `// Allocation-free hot path` comment
  is accurate). The decision gate (allocation hotspot must surface AND wall-clock A/B
  must clear ±1 % at N = 18 → ship; otherwise abandon) returned the negative branch.
  The phase-1 sample DFS (`CollectAllSampleSolutionsDFS`) does allocate a single
  `int[N]` per call and `int[N]` per solution copy at N > 25, but terminates within
  milliseconds (capped at `MaxDisplayedCount` samples) and is invisible against the
  99.99 % Self of `Search`. The new `AllModeMaterializeAllocationBenchmark` stays as a
  permanent regression guard for the All-mode materialize allocation surface,
  irrespective of the experiment's outcome. Branch ships docs-only.
- `perf/cached-diagonal-shifts` (PR #16) — Candidate queue #2 closed as a profile-first
  negative finding (docs-only branch, no production-code changes). Opened off
  freshly-merged `main` (post-PR #15) to decide whether `(d1|bit)<<1` / `(d2|bit)>>1` in
  `CountCanonicalDFS` (`BitmaskSolver.CountUnique.cs:207`) should be replaced with a
  per-row cached shifted-mask table.
  N = 16 ≈ 195.7 ms ±0.63 %, N = 17 ≈ 1,416.3 ms ±0.53 %. Line-level CPU attribution via
  `profile_unit_test` against
  `BitmaskSolverCountUniqueTests.CountUniqueAdaptive_PreservesPruningFlags(n: 16, …)`:
  **91.78 % Total inside the `Parallel.ForEach` body → recursive `CountCanonicalDFS`**,
  with per-frame Self% peaking at 14.66 % (col 5), 13.85 % (col 6), 12.05 % (col 7). The
  only non-trivial leaf was `BitOperations.TrailingZeroCount` at 5.17 % Self. **The
  diagonal shifts did not surface as a separate sample band** — they fold into the
  bit-scan loop's per-frame Self alongside `avail ^= bit`, `rows[col] = r`, and the
  prune-gate branch. Decision gate (≥ 2–3 % Self attributable specifically to the shifts
  → write the cache; < 1 % or folded → abandon) returned the negative branch. The
  skeptical prior in the original Candidate queue #2 entry is now empirically confirmed:
  because `bit = avail & -avail` is recomputed and unique per iteration, a per-iteration
  "cache" can only rename the two ALU ops, not amortise them. Branch ships docs-only.
- Unique-mode parallel count-only partitioner swap (`perf/all-work-stealing`, same commit
  as the All-mode entry below): apply the identical chunk-of-1
  `Partitioner.Create(items, NoBuffering)` swap to the depth-2 dispatch in
  `BitmaskSolver.CountUniqueFastHalfBoard` (`BitmaskSolver.CountUnique.cs:90`). A/B under
  `UniqueFastHalfBoardEvenOddBenchmark` (full job, 3 warmups / 15 iterations): N = 16
  -21.3 % (248.9 -> 195.8 ms), N = 17 -31.1 % (2,059.2 -> 1,419.5 ms), CIs non-overlapping at
  both Ns. The N = 17 delta is in fact *larger* than the All-mode N = 18 delta because
  fewer total items relative to 28 logical cores makes the static-partitioner imbalance
  worse to start with. 513 / 513 tests green across `NQueen.UnitTests` + `NQueen.ViewModelTests`.
  Code change: ~3 lines + a comment in `BitmaskSolver.CountUnique.cs`. The companion
  engine-path `Parallel.ForEach` in `BitmaskParallelEngine.Unique.cs` is deliberately
  untouched (only reachable for N < 16 count-only or for materialize/visualize paths).
- All-mode parallel count-only partitioner swap (`perf/all-work-stealing`): wrap the
  existing depth-2 work-item array in `Partitioner.Create(items, NoBuffering)` inside
  `BitboardNQueenSolver.CountSolutions` so each worker pulls one item at a time instead of
  the default static range partitioning. Per-item cost spans orders of magnitude (centre-row
  first queens vs edge-row), so the dynamic dispatcher recovers tail-imbalance idle time.
  A/B under new `AllCountOnlyParallelScalingBenchmark` (full job, 3 warmups / 15
  iterations): N = 16 -17.0 % (182.0 -> 151.0 ms), N = 18 -24.1 % (9.74 -> 7.39 s), CIs
  non-overlapping at both Ns. Throughput at N = 18 on 28 logical cores: ~68 M -> ~90 M
  solutions/sec. 424 / 424 unit tests green; Unique-mode regression-guard benchmark
  unaffected (Unique path is untouched). Code change: ~5 lines + a comment in
  `BitboardNQueenSolver.cs` plus the new benchmark harness in `AllModeBenchmarks.cs`.
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

> _All three original Candidate-queue items are resolved (**Depth-based work-stealing
> queue (All mode)** shipped on `perf/all-work-stealing` / PR #15; both **Iterative core
> for the Unique hot path** and **Cached shifted diagonal masks** abandoned as
> profile-first negative findings). The cross-listed **`ArrayPool<T>` for column /
> diagonal / row stacks** is likewise resolved (abandoned on `perf/all-mode-arraypool`
> as the third profile-first negative finding in a row). The list-level
> **Symmetry reduction in All count-only path** is also resolved (abandoned on
> `perf/all-mode-symmetry-reduction` as the fourth profile-first negative finding;
> structural argument from code reading — see *Recently shipped* and
> `CHANGELOG.md [Unreleased] → Docs`). The list below is the wider perf inventory for
> the next picker._

- ~~**Symmetry reduction in All count-only path** beyond the existing half-board
  restriction (enumerate fundamental representatives and expand by symmetry factor).~~
  _Abandoned 2026-06-12 on `perf/all-mode-symmetry-reduction` — see *Recently shipped*
  and the `[Unreleased] → Docs` CHANGELOG entry for the structural argument. The
  half-board restriction `row0 ∈ [0, N/2)` on even N already exhausts the row-reflection
  prune (`ShouldPrunePrefixFull`'s loop breaks at `i = 0` for every node at the
  benchmark sizes N = 16 / 18); the remaining D4 factor of up to 4× lives in rotation
  symmetries that are not column-preserving and require leaf-level canonical-form
  checking — pure overhead on a 99.99 %-Self register-tight path._
- ~~**`ArrayPool<T>`** for column / diagonal / row stacks to reduce GC pressure at N ≥ 18.~~
  _Abandoned 2026-06-11 on `perf/all-mode-arraypool` — see *Recently shipped* and the
  `[Unreleased] → Docs` CHANGELOG entry for the three-measurement trace evidence. The
  All-mode materialize hot path is `BitboardNQueenSolver.Search`, an explicitly
  allocation-free static DFS (99.99 % Self in the supplementary CPU trace); the
  `int[]` / `Frame[]` candidates for pooling are reached only on a millisecond-scale
  phase-1 sample DFS that is invisible against `Search`._
- **Iterative core for All mode** — port Unique's allocation-free iterative DFS.
- **MRV heuristic** for next-column branch ordering (cost / benefit needs benchmarking).
- ~~**Cached shifted diagonal masks** per row to remove repeated `(d1|bit)<<1` /
  `(d2|bit)>>1` in the hottest loop.~~ _Abandoned 2026-06-10 on `perf/cached-diagonal-shifts`
  — see Candidate queue #2 entry above for trace evidence._

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
