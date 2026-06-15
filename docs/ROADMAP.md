# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Next session — start here

> Updated when `perf/all-mode-iterative-search-bounds-elision` opened off `main` at
> `f1552ac` (post-PR #23: the docs-only closure of `perf/all-mode-mrv-heuristic` as
> the **fifth profile-first negative finding in a row** on the All count-only
> `BitboardNQueenSolver.Search` code path). Branch baseline measurement is the next
> action; no production code touched yet. The MEASURE-first investigation should
> establish profile-time evidence (or reuse the recent post-PR #20 measurement, fresh
> within ±1 % at both Ns) *before* any production code change — see *Decision gate*
> in the *Step 2.4 — starting brief* below.

**Active branch — `perf/all-mode-iterative-search-bounds-elision`.** Just opened off
`main` at `f1552ac`. Picks up the secondary signal that surfaced from the Step 2.3
(MRV) kill check: `Span<Frame>.get_Item` at **66.36 % Self** in the iterative
`Search` body — the bounds-checked frame-load on backtrack at
`BitboardNQueenSolver.cs:200` — is the **largest single-function Self attribution
ever documented on this code path** (2.6× the entire `Search` body's combined Self;
for comparison, the strongest prior signal was `BitOperations.TrailingZeroCount` at
5.17 % Self on `perf/cached-diagonal-shifts`, still abandoned because the leaf was
register-cheap once the `Bmi1.X64.TrailingZeroCount` intrinsic substitution was
tried). The fix shape is well-trodden: `MemoryMarshal.GetReference(stack)` once +
`Unsafe.Add(ref head, row - startRow)` for both the read on backtrack and the write
on push — the canonical RyuJIT bounds-elision pattern, ~3 lines. Calibrated prior
**~50–70 % positive** for clearing the > 1 % wall-clock gate at N = 18 — substantially
higher than any prior candidate on this path. Risk shape is correctness, not perf
(an `Unsafe`-based pointer walk that goes out of bounds on backtrack would be a
genuine memory-safety bug), so the oracle gate plus the existing 535-test suite
plus a debug-only bounds-assert have to hold.

For the full closure rationale of the just-merged `perf/all-mode-mrv-heuristic`
(Step 2.3) see `CHANGELOG.md [Unreleased] → Docs` (the closure entry shipped via
PR #23) and the in-tree archived profile evidence at
`NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/kill-check-mrv-heuristic.md`.

**Execution queue — progress.** Step 1 (Documentation drift housekeeping) shipped as
PR #18. Step 2.1 (Symmetry reduction in All count-only path) closed as the fourth
profile-first negative finding (PR #19). Step 2.2 (Iterative core for All mode) shipped
as **PR #20** — the first positive after four negatives (-3.0 % at N = 18, -3.1 % at
N = 16, both with non-overlapping 99.9 % CIs; 535 / 535 tests green). Step 2.3 (MRV
heuristic) closed as the **fifth profile-first negative finding** via PR #23
(squash `f1552ac`, docs-only). Step 2.4 — `Span<Frame>.get_Item` **bounds-check
elision** in the iterative `Search` body — surfaced from the Step 2.3 kill-check
profile and is the **active perf branch** (`perf/all-mode-iterative-search-bounds-elision`).
Then 3 (Investigations — Unique CountOnly vs Materialize gap at N = 17–19), then 4
(Test Coverage closeout).

**Step 2.4 — starting brief (`Span<Frame>.get_Item` bounds-check elision in iterative
`Search`).**

*What surfaced.* The Step 2.3 kill-check profile against
`HalfBoardFlagAllModeTests.CountOnly_AllMode_FlagOn_MatchesExpected(n: 16)` returned
this hot path:

```
BitboardNQueenSolver.<>c__DisplayClass0_0.<CountSolutions>b__4   91.92 % Total /  0.08 % Self
└─> BitboardNQueenSolver.Search                                  91.84 % Total / 25.48 % Self
    └─> Span<Frame>.get_Item(int)                                66.36 % Total / 66.36 % Self [HOT]
```

`Span<Frame>.get_Item` at **66.36 % Self** is **2.6× the entire `Search` body's
combined Self** — a single indexer eclipses every register-tight ALU op in the function.
Each `stack[row - startRow]` read inside the `if (available == 0)` backtrack branch
(`BitboardNQueenSolver.cs:200`) runs the JIT-emitted bounds check; for an N = 18
workload that fires across the ~91 G-node search tree, so the bounds check itself is
where the runtime is going.

*Why this is genuinely new.* The four prior negatives on this path closed because the
candidate optimization's target line did not surface as a concentrated band. **This
candidate's target line is at 66.36 % Self.** That is the **largest single-function
Self attribution** ever documented on this code path — for comparison, the strongest
prior signal was `BitOperations.TrailingZeroCount` at 5.17 % Self on
`perf/cached-diagonal-shifts` (still abandoned because the leaf was register-cheap once
the `Bmi1.X64.TrailingZeroCount` intrinsic substitution was tried).

*Decision gate (fixed up front).* Identical to PR #20's pattern:
  1. **Oracle** — the bounds-check-elided variant must equal `CountSolutions` for every
     N ∈ [1, 14] across both `parallel: true` and `parallel: false`. Reuse / extend the
     existing `BitboardNQueenSolverTests.CountSolutions_{Parallel,Sequential}_MatchesRecursive`
     theories.
  2. **Perf (primary)** — at N = 18, the elided variant must show a wall-clock improvement
     over the post-PR #20 iterative baseline (`branch-baseline-all-mode-mrv-heuristic.md`:
     7,043.0 ms ±0.44 %) of **> 1 %** with **non-overlapping 99.9 % CIs** in a new
     `AllCountOnlyBoundsElisionVsBaselineBenchmark` (full job: 3 warmups, 15 iterations,
     both cells in the same job).
  3. **Non-regression** — at N = 16 the elided variant must not regress vs the same
     baseline (140.4 ms ±0.66 %) by more than 1 %.

*Prior probability — high (relative to the rest of the backlog).* The signal is
unambiguous and the fix shape is well-trodden: replace `Span<Frame> stack` indexer
access with `ref Frame head = ref MemoryMarshal.GetReference(stack)` once, then read /
write via `Unsafe.Add(ref head, row - startRow)` (or the equivalent on
`stackalloc Frame[]` via `Unsafe.AsRef`). This is the canonical RyuJIT bounds-check
elision pattern and is used elsewhere in the BCL hot paths the project depends on.
Calibrated guess: **~50–70 % positive probability** for clearing the > 1 % wall-clock
gate at N = 18 — substantially higher than any prior candidate on this path. The risk
is correctness (an `Unsafe`-based pointer walk that goes out of bounds on backtrack
would be a genuine memory-safety bug, not just a perf regression), so the oracle gate
plus the existing 535-test suite plus the new bounds-check assertions in DEBUG builds
have to hold.

*Two reasonable structural shapes.*
  - **Variant A — `MemoryMarshal.GetReference` + `Unsafe.Add`.** Get the Span's base
    `ref Frame` once at the top of `Search`; replace every `stack[row - startRow]`
    access (one read on backtrack, one write on push) with `Unsafe.Add(ref head,
    row - startRow)`. Minimal code change (~3 lines). The bounds-check is elided
    statically since `row - startRow` is bounded by the Span's known length.
  - **Variant B — fully raw `Frame*` over the stackalloc'd buffer.** Allocate via
    `Frame* stack = stackalloc Frame[n - startRow]`, work entirely in pointer arithmetic
    inside an `unsafe` block. Maximally fast but loses the Span debugger ergonomics and
    requires `unsafe` keyword on the method. Implement only if Variant A clears the
    gate but a measurable margin remains on the table.

If Variant A's profile shows the bounds check vanished (the new
`<>c__DisplayClass0_0.<CountSolutions>b__4` Self should rise sharply, the `Span` indexer
should disappear from the top-N) **and** the wall-clock gate clears, Variant A ships
as the production hot path with the existing iterative form retained as
`internal SearchBoundsChecked` for the new A/B benchmark. If the bounds check vanishes
in profile but wall-clock doesn't move, that's a sixth negative finding — still useful
data, since it says the bounds check is JIT-emitted-but-branch-predicted-out and the
Self attribution is misleading; close docs-only mirroring this branch's pattern.

*Re-baseline before any production change.* The post-PR #20 baseline in
`branch-baseline-all-mode-mrv-heuristic.md` (N = 16 = 140.4 ms ±0.66 %, N = 18 =
7,043.0 ms ±0.44 %) is fresh enough to reuse if the next session opens within ~1 week
on the same hardware / environment. If longer, re-run `AllCountOnlyParallelScalingBenchmark`
on freshly-merged `main` to capture a new baseline doc.

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
| Active branch | `perf/all-mode-iterative-search-bounds-elision` — just opened off `main` at `f1552ac` (post-PR #23: the docs-only closure of `perf/all-mode-mrv-heuristic` as the fifth profile-first negative finding in a row). State: **branch baseline measurement is the next action**; no production code touched yet. Step 2.4 of the perf execution queue: pick up the secondary signal that surfaced from the Step 2.3 (MRV) kill-check profile — `Span<Frame>.get_Item` at **66.36 % Self** in the iterative `Search` body, the bounds-checked frame-load on backtrack at `BitboardNQueenSolver.cs:200`. Largest single-function Self attribution ever documented on this code path (2.6× the entire `Search` body's combined Self). Fix shape: `MemoryMarshal.GetReference(stack)` + `Unsafe.Add(ref head, row - startRow)` (canonical RyuJIT bounds-elision pattern, ~3 lines). Calibrated prior **~50–70 % positive** for clearing the > 1 % wall-clock gate at N = 18 — substantially higher than any prior candidate on this path. Decision gate (fixed up front, mirroring PR #20): oracle parity at N ∈ [1, 14] across both `parallel: true` and `parallel: false` AND > 1 % wall-clock improvement at N = 18 with non-overlapping 99.9 % CIs AND non-regression at N = 16. Risk shape is correctness, not perf (an `Unsafe` walk that goes out of bounds on backtrack would be a memory-safety bug). Full brief in *Next session — start here*. The previous branch `perf/all-mode-mrv-heuristic` closed as the **fifth profile-first negative finding** via PR #23 (squash `f1552ac`); the perf-track positives remain PR #15 (-17 to -24 % from the chunk-of-1 partitioner) and PR #20 (-3.0 % at N = 18 / -3.1 % at N = 16 from the iterative `Search` body + leaf-shortcut). |
| Target framework | .NET 10 across all projects (`net10.0` / `net10.0-windows` for GUI) |
| Test count | **535 / 535 passing** (446 unit + 89 view-model). Up from 515 pre-branch because this branch added 20 new parity tests in `BitboardNQueenSolverTests` (CountSolutions_{Parallel,Sequential}_MatchesRecursive theories + CountSolutionsRecursive_OutOfRange_Throws). |
| Code coverage | Stale (last full run 2026-05-29: Domain 93 %, Kernel 67 %, Shared 95 %, Total 77 %). Re-collect pending. |
| Build status | 0 errors / 0 warnings |

### Recently shipped (see `CHANGELOG.md` `[Unreleased]` for full detail)

- `perf/all-mode-mrv-heuristic` — "MRV (Minimum Remaining Values) heuristic for
  next-column branch ordering" candidate (from `Backlog → Larger wins, scoped risk`;
  Step 2.3 of the execution queue) closed as the **fifth profile-first negative finding
  in a row** (docs-only branch, no production-code changes). Opened off freshly-merged
  `main` (post-PR #22, `d4b26cc`) to *decide* whether the iterative
  `BitboardNQueenSolver.Search` should pick the next column whose
  `available = ~(cols | d1 | d2) & mask` has the fewest set bits — classical CSP
  fail-fast — rather than walking columns 0..N-1 in fixed order. Two structural shapes
  on the table: Variant A (column reorder fixed at root, cheap per-iteration), Variant B
  (dynamic per-level recompute). Branch baseline
  `branch-baseline-all-mode-mrv-heuristic.md` re-established at N = 16 = 140.4 ms
  ±0.66 %, N = 18 = 7,043.0 ms ±0.44 % (both within ±1 % of the post-PR #20 iterative
  reference; the prior session's environmental-regression suspicion confirmed an
  artifact, not a code regression). **The kill signal arrived from line-level CPU
  attribution before any production change** via `profile_unit_test` against
  `HalfBoardFlagAllModeTests.CountOnly_AllMode_FlagOn_MatchesExpected(n: 16)` — the
  production All-mode count-only dispatch end-to-end. Function-level rollup:
  `BitboardNQueenSolver.Search` body 91.84 % Total / **25.48 % Self** combined across
  all eight inline ops in the loop body (the `if (available == 0)` backtrack pop, LSB
  extraction, bit-clear, leaf-shortcut, frame push, diagonal-shift descend block,
  `row++`, and the per-row `available = ~(cols | d1 | d2) & mask` line MRV would
  manipulate); `Span<Frame>.get_Item(int)` at **66.36 % Self** [HOT]. Eight inline ops
  sharing 25.48 % Self gives a per-op average around 3.2 % at most, with a heavily
  uneven actual distribution — and even on the most generous read, **no single inline
  op including the `available = …` line plausibly clears 2–3 % Self attributable
  specifically to it**. The line folds into the bit-scan loop's per-iteration Self
  exactly the way the diagonal shifts `(d1|bit)<<1` and `(d2|bit)>>1` did on
  `perf/cached-diagonal-shifts` (PR #16) — the failure mode the kill criterion was
  written to detect. Beyond line attribution, the dominant cost (`Span<Frame>.get_Item`
  at 66.36 % Self, **2.6× the entire `Search` body's combined Self**) is cleanly
  outside MRV's lever entirely: MRV changes which column is branched next but does not
  reduce frames pushed onto the stack, and would *add* per-level popcount work onto
  `Search`'s body — `(N - d)` popcount calls plus a min-reduction at depth `d`,
  4–8 popcounts per descent at N = 16 — each touching state that would otherwise stay
  in registers. Pattern reasserts: positives on this path require either *removing a
  per-leaf operation entirely* (PR #20's leaf-shortcut) or *re-targeting a structural
  inefficiency off the hot path* (PR #15's chunk-of-1 partitioner moved tail-imbalance
  work *off* `Search`); adding work *on* the hot path — even with plausible pruning
  upside — has now failed twice in identical fashion (this branch and PR #16).
  **Secondary discovery (deliberately out of scope but recorded for the perf backlog):**
  `Span<Frame>.get_Item` at 66.36 % Self is the **largest single-function Self
  attribution ever documented on this code path** — for comparison the strongest prior
  signal was `BitOperations.TrailingZeroCount` at 5.17 % Self on
  `perf/cached-diagonal-shifts`. Seeds Step 2.4 (`Span<Frame>.get_Item` bounds-check
  elision) as the next active perf candidate; full brief in *Next session — start
  here*. No production-code changes; no new benchmark (the existing
  `AllCountOnlyParallelScalingBenchmark` and `AllCountOnlyRecursiveVsIterativeBenchmark`
  from PR #15 / PR #20 already serve as permanent regression guards). Branch baseline
  doc and kill-check evidence doc both stay in tree under
  `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/` (gitignored) as archived
  evidence. 535 / 535 tests stay green. Branch ships docs-only.
- `perf/all-mode-iterative-core` (PR #20, squash `9352187`) — "Iterative core for All
  mode" candidate (from `Backlog → Larger wins, scoped risk`) shipped as the **first
  positive profile-driven finding after four consecutive negatives** (production-changing
  branch). Opened off freshly-merged `main` (post-PR #19, `0582c13`) to *measure*
  whether porting `BitboardNQueenSolver.Search` from the recursive bit-mask DFS to an
  allocation-free iterative form (modelled on `BitmaskSearchEngine.MainLoopCountOnly`'s
  pattern) would clear the ±1 % wall-clock decision gate at N = 18.
  `branch-baseline-all-mode-iterative-core.md` on `3cd7162`: `AllCountOnlyParallelScalingBenchmark`
  N = 16 ≈ 147.2 ms ±0.6 %, N = 18 ≈ 7,359.4 ms ±1.02 %. New A/B harness
  `AllCountOnlyRecursiveVsIterativeBenchmark` (both cells in the same 3-warmup /
  15-iteration job, recursive `Baseline = true` so BDN reports the iterative ratio
  directly): **N = 16 = 143.8 → 139.3 ms (-3.1 %, ratio 0.97)**, CIs 99.9 % [142.6, 145.0]
  vs [138.5, 140.1] non-overlapping by 2.5 ms; **N = 18 = 7,314.9 → 7,098.5 ms (-3.0 %,
  ratio 0.97)**, CIs 99.9 % [7,258.2, 7,371.6] vs [7,075.0, 7,122.0] non-overlapping by
  136.2 ms. All three decision gates clear: (1) oracle parity via 20 new parity tests at
  N ∈ [1, 14] across both `parallel: true` and `parallel: false`; (2) > 1 % wall-clock
  improvement at N = 18 with non-overlapping CIs; (3) non-regression at N = 16 — in fact
  a 3.1 % improvement. The iterative form replaces the recursive call stack with
  `Span<Frame> stack = stackalloc Frame[n - startRow]` (max 32 × 32 B = 1 KB on the stack,
  comfortably below default thread-stack reservations) and adds a leaf-shortcut
  (`if (row + 1 == n) { count++; continue; }`) that skips the frame push for terminal
  rows. Plausible structural reason for the win: the contiguous Span keeps the working
  set L1/L2-friendly versus RyuJIT's recursive frame, and the leaf-shortcut saves one
  frame push / pop per leaf solution — at N = 18 (~91 G nodes after half-board reduction)
  that's a meaningful amortised reduction. Production swap implemented as a **rename**:
  iterative `SearchIterative` → `Search` (production hot path under its original name),
  recursive `Search` → `SearchRecursive` (kept `internal` so the new A/B benchmark and
  the parity tests can reach it via `InternalsVisibleTo NQueen.Benchmarking` +
  `NQueen.UnitTests`). All four production call sites of `CountSolutions` were
  untouched. The new `AllCountOnlyRecursiveVsIterativeBenchmark` + `CountSolutionsRecursive`
  internal mirror stay as permanent regression guards: any future change that
  re-introduces recursion (or otherwise inverts the ratio) shows up here against the
  in-tree reference baseline. 535 / 535 tests green across `NQueen.UnitTests` +
  `NQueen.ViewModelTests`. Files changed: `NQueen.Kernel/Solvers/BitboardNQueenSolver.cs`
  (the rename + iterative `Search` body + `Frame` record-struct + internal
  `CountSolutionsRecursive` mirror), `NQueen.Kernel/NQueen.Kernel.csproj` (added
  `InternalsVisibleTo NQueen.Benchmarking`), `NQueen.Benchmarking/AllModeBenchmarks.cs`
  (new `AllCountOnlyRecursiveVsIterativeBenchmark`),
  `NQueen.UnitTests/Tests/Kernel/BitboardNQueenSolverTests.cs` (the +20 new parity-test
  theories).
- `perf/all-mode-symmetry-reduction` (PR #19) — "Symmetry reduction in All count-only path"
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
> `CHANGELOG.md [Unreleased] → Docs`). The **MRV heuristic** is also resolved
> (abandoned on `perf/all-mode-mrv-heuristic` as the fifth profile-first negative
> finding; line-level evidence — see *Recently shipped*). The list below is the wider
> perf inventory for the next picker._

- **`Span<Frame>.get_Item` bounds-check elision in iterative `Search`** — surfaced from
  the `perf/all-mode-mrv-heuristic` kill-check profile (Step 2.3 closure). At N = 16,
  `Span<Frame>.get_Item(int)` runs at **66.36 % Self** — 2.6× the entire `Search` body's
  combined Self. The bounds check on `stack[row - startRow]` (read on backtrack at
  `BitboardNQueenSolver.cs:200`, write on push at line 215) fires across the ~91 G-node
  search tree at N = 18. Largest single-function Self attribution ever documented on
  this code path. Calibrated prior **~50–70 % positive** for clearing the > 1 %
  wall-clock gate at N = 18 — substantially higher than any prior candidate on this
  path. Variant A: `MemoryMarshal.GetReference(stack)` once + `Unsafe.Add(ref head,
  row - startRow)` for both read and write (~3 lines, RyuJIT canonical bounds-elision
  pattern). Variant B: fully raw `Frame*` over `stackalloc Frame[]` (`unsafe` block,
  loses Span debugger ergonomics — only if Variant A leaves a measurable margin on the
  table). Decision gate identical to PR #20 (oracle parity at N ∈ [1, 14] across both
  parallel modes; > 1 % wall-clock improvement at N = 18 with non-overlapping 99.9 % CIs;
  non-regression at N = 16). Risk shape is correctness, not perf — an `Unsafe`-based
  pointer walk that goes out of bounds on backtrack would be a memory-safety bug, so
  the oracle gate plus the existing 535-test suite plus a debug-only bounds-assert have
  to hold. **Step 2.4 of the execution queue.** Full brief in *Next session — start
  here*.
- ~~**MRV heuristic** for next-column branch ordering~~ _Abandoned 2026-06-15 on
  `perf/all-mode-mrv-heuristic` — see *Recently shipped* and the `[Unreleased] → Docs`
  CHANGELOG entry for the line-level CPU attribution evidence. The per-row
  `available = ~(cols | d1 | d2) & mask` line did not surface as its own ≥ 2–3 % Self
  band (folded into the iterative `Search` body's combined 25.48 % Self across all
  eight inline ops, identical pattern to the cached-diagonal-shifts negative on
  PR #16); MRV would add per-level popcount work onto an already-small bit-mask body
  while leaving the dominant 66.36 % `Span<Frame>.get_Item` cost untouched. Fifth
  profile-first negative finding in a row on this code path._
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
- ~~**Iterative core for All mode** — port Unique's allocation-free iterative DFS.~~
  _Shipped 2026-06-09 on `perf/all-mode-iterative-core` — see *Recently shipped* and the
  `[Unreleased] → Performance` CHANGELOG entry. **First positive profile-driven finding
  after four consecutive negatives** (-3.0 % at N = 18, -3.1 % at N = 16, both with
  non-overlapping 99.9 % CIs)._
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
