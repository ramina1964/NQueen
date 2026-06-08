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
> 1. **MEASURE first ✅ done (2026-06-08).** Re-ran `UniqueFastHalfBoardEvenOddBenchmark`
>    on freshly-merged `main` (`f75c5ea`) under the full job (3 warmups, 15 iterations).
>    New authoritative baseline on the dev machine (i7-14700K, 28 logical / 20 physical):
>    **N=16 ≈ 254.8 ms ±1.25 %, N=17 ≈ 2,103.0 ms ±0.93 %**. About 3–4 % slower than the
>    pre-event-migration target (244 / 2,042 ms) — small but outside the previous ±1 %
>    band, so the table-of-record numbers are updated below to match what re-runs on
>    `main` will see.
> 2. **Capture a line-level / instruction-level CPU profile ✅ trace captured (2026-06-08).**
>    The same benchmark was re-run with `[CPUUsageDiagnoser]` to drop a CPU sampling ETL
>    next to the results at
>    `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/BenchmarkDotNet_UniqueFastHalfBoardEvenOddBenchmark_20260608_213732/1804E697-BC82-40D3-95F7-7D72D3B9E9D5/sc.user_aux.etl(x)`.
>    `analyze_perf_trace` returned no findings against the raw ETL, so we pivoted to
>    `profile_unit_test` against `BitmaskSolverCountUniqueTests.CountUniqueAdaptive_PreservesPruningFlags(n: 16, …)`
>    for line-level attribution. Result: ~90 % Total inside the `Parallel.ForEach` body →
>    `CountCanonicalDFS` recursion, with the deepest two frames (cols 13–14) carrying
>    16–19 % Self each, cols 10–12 carrying 6–17 % Self each. The single non-trivial leaf
>    that surfaced was `BitOperations.TrailingZeroCount` at **5.01 % Self** — out-of-line
>    despite its `[AggressiveInlining]` attribute. No call/ret/prologue/epilogue bucket
>    was visible as a separate sample group at all (bucket **(b)** was empirically near
>    zero).
> 3. **Decide ✅ done — negative finding (2026-06-08).** Bucket **(b)** never materialised
>    in the trace, so the original ≥ 8 % gate cannot trigger. To rule out the only
>    candidate that *did* show up — the 5 % TZCNT leaf — we ran a one-line pre-experiment
>    (Option C-2): replaced the `BitOperations.TrailingZeroCount` call in the hot loop
>    with a hand-routed `Bmi1.X64.TrailingZeroCount` intrinsic via a `[AggressiveInlining]`
>    `Tzcnt64` helper. Re-profile confirmed the leaf physically disappeared (RyuJIT
>    inlined the helper) and the recovered cycles redistributed cleanly into the
>    surrounding cols-10..14 frames. **Wall-clock did not move** —
>    N = 16 = 256.557 ms ±1.11 % vs baseline 254.8 ms ±1.25 % (+0.7 %, fully inside both
>    noise bands); N = 17 short run produced 2.1253 s and 2.0979 s before the diagnoser
>    hung, both inside baseline 2,103.0 ms ±0.93 %. The 5 % attribution was sampling
>    noise around the call site, not real wall-clock work the JIT could remove. The C-2
>    change was reverted (no production code shipped on this branch); the iterative-DFS
>    port is **abandoned** because the bucket it would target does not exist on this
>    workload.
> 4. **No production-code changes on this branch.** The branch carries only docs +
>    profiling artefacts (`BenchmarkDotNet.Artifacts/`). Closing `perf/unique-iterative-core`
>    with the negative finding above; the next branch picks up **work-stealing for All mode**
>    (Candidate queue #3 below) per the documented tail-imbalance evidence in
>    `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20`.
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
   — **next branch** once `perf/unique-iterative-core` is closed.

**Process (rule).** Branch off freshly-merged `main` with a name tied to the *specific*
experiment (e.g. `perf/unique-iterative-core`, not a generic "small-wins" name) so the
branch can't over-promise. Run `UniqueFastHalfBoardEvenOddBenchmark` to re-establish the
baseline before touching production code, per the team's MEASURE-first practice.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `perf/unique-iterative-core` (deferred-perf candidate #1, **closing — negative finding, no production-code changes shipped**: profile-first investigation completed, no recoverable bucket exists. Steps 1–2 ✅ baseline + ETL. Step 3 ✅ — `profile_unit_test` showed the call/ret bucket is empirically near-zero; the only leaf in the trace, `BitOperations.TrailingZeroCount` at 5.01 % Self, was disproved by a Tzcnt64-intrinsic pre-experiment (RyuJIT inlined the helper, the leaf vanished, wall-clock unchanged at N = 16 = +0.7 % within ±1.11 % noise). The C-2 change was reverted; the iterative-DFS port is abandoned. Next branch picks up Candidate queue #3, **work-stealing for All mode**.). `main` at `f75c5ea` (PR #13 `refactor/event-migration` event→push-sink migration squash-merged). |
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
