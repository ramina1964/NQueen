# Changelog

All notable changes to this project are documented here.

---

## [Unreleased]

### Performance
- **Kernel performance — `BitboardNQueenSolver.CountSolutions` All-mode count-only DFS
  core ported from recursive to iterative.** Branch `perf/all-mode-iterative-core` off
  freshly-merged `main` (post-PR #19, `0582c13`); step 2.2 of the execution queue (from
  `Backlog → Larger wins, scoped risk`). The recursive `Search` walked the bit-mask DFS via
  ~10 G–90 G `call` / `ret` pairs on top of its useful work for N = 16 / N = 18; the
  iterative port keeps the bit-mask state in registers and replaces the call stack with a
  `Span<Frame> stack = stackalloc Frame[n - startRow]` (max 32 × 32 B = 1 KB on the stack,
  comfortably below any default thread-stack reservation), with a leaf-shortcut
  (`if (row + 1 == n) { count++; continue; }`) that skips the frame push for terminal rows.
  Modelled on the existing iterative pattern in `BitmaskSearchEngine.MainLoopCountOnly`.
  Same control flow at every dispatch site (parallel N ≥ 14 partitioner branch, parallel
  N < 14 simple split, sequential, odd-N centre-row tail), same per-thread `Interlocked.Add`
  reduction — behaviour-identical (oracle gate: 20 / 20 parity tests at N ∈ [1, 14] across
  both parallel and sequential). **A/B measurement (new
  `AllCountOnlyRecursiveVsIterativeBenchmark`, full job: 3 warmups, 15 iterations, both
  cells in the same job for environment-controlled comparison):** N = 16 = 143.8 ms ->
  139.3 ms (**-3.1 %**, ratio 0.97; CIs 99.9 % [142.6, 145.0] vs [138.5, 140.1],
  non-overlapping by 2.5 ms); N = 18 = 7,314.9 ms -> 7,098.5 ms (**-3.0 %**, ratio 0.97;
  CIs 99.9 % [7,258.2, 7,371.6] vs [7,075.0, 7,122.0], non-overlapping by 136.2 ms; 1
  outlier of 7.16 s removed from the iterative cell). N = 18 throughput moved from ~91.6 M
  to ~94.4 M solutions/sec on 28 logical cores. The decision gate (oracle parity at N ∈ [1,
  14] AND > 1 % wall-clock improvement at N = 18 with non-overlapping 99.9 % CIs AND
  non-regression at N = 16) cleared all three checks decisively. Plausible structural
  reason: the contiguous `Span<Frame>` keeps the working set small and L1/L2-friendly
  versus RyuJIT's recursive call frame, and the leaf-shortcut saves one frame push / pop
  per leaf solution — at N = 18 (~91 G nodes after half-board reduction) that's a
  meaningful amortised reduction. **First positive profile-driven finding after four
  consecutive negatives** (`perf/unique-iterative-core`, `perf/cached-diagonal-shifts`,
  `perf/all-mode-arraypool`, `perf/all-mode-symmetry-reduction`). Production swap
  implemented as a rename rather than a method-name change: iterative `SearchIterative` →
  `Search` (the production name is preserved), recursive `Search` → `SearchRecursive`
  (kept `internal` so `AllCountOnlyRecursiveVsIterativeBenchmark` and the parity tests can
  reach it via `InternalsVisibleTo`). All four production call sites of `CountSolutions`
  (`BitmaskSolver.All.cs:79`, `BitmaskSolver.All.cs:111`, `BitmaskSolver.cs:280`,
  `NQueenBench.cs:11`) were untouched. Correctness preserved (535 / 535 tests green across
  `NQueen.UnitTests` + `NQueen.ViewModelTests`, including the +20 new parity tests:
  `BitboardNQueenSolverTests.CountSolutions_Parallel_MatchesRecursive(n: 1..14)` × 14,
  `BitboardNQueenSolverTests.CountSolutions_Sequential_MatchesRecursive(n: 1, 4, 5, 8, 11,
  13)` × 6, `BitboardNQueenSolverTests.CountSolutionsRecursive_OutOfRange_Throws(n: 0, 33)`
  × 2). The recursive baseline + new A/B benchmark stay as permanent regression guards.
  Files changed: `NQueen.Kernel/Solvers/BitboardNQueenSolver.cs` (the rename + iterative
  `Search` body + Frame record-struct + internal `CountSolutionsRecursive` mirror),
  `NQueen.Kernel/NQueen.Kernel.csproj` (added `InternalsVisibleTo NQueen.Benchmarking`),
  `NQueen.Benchmarking/AllModeBenchmarks.cs` (new
  `AllCountOnlyRecursiveVsIterativeBenchmark`),
  `NQueen.UnitTests/Tests/Kernel/BitboardNQueenSolverTests.cs` (new parity-test theories).
- **Kernel performance — `BitboardNQueenSolver.CountSolutions` All-mode count-only parallel
  dispatch switched to a chunk-of-1 dynamic partitioner.** Branch `perf/all-work-stealing`
  off `main` (`f75c5ea`); Candidate queue #3 from the deferred perf track (source:
  `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20`). The N >= 14 branch of
  `CountSolutions` previously fed the depth-2 work-item array directly to
  `Parallel.ForEach(items, ...)`, which uses a static range partitioner; on the dev machine
  (i7-14700K, 28 logical / 20 physical cores) that left tail stragglers because per-item
  cost spans orders of magnitude — centre-row first queens generate far larger subtrees
  than edge-row ones, so equal-count ranges produce wildly unequal wall-clock. The fix
  wraps the items array in
  `Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering)` so each worker
  pulls one item at a time and fast workers help drain the heavy items. Same items, same
  `Search` recursion, same per-thread reduction via `Interlocked.Add` — behaviour-identical.
  **A/B measurement (new `AllCountOnlyParallelScalingBenchmark`, full job: 3 warmups, 15
  iterations):** N = 16 = 182.0 ms -> 151.0 ms (**-17.0 %**, CI 99.9 % [180.1, 183.9] ->
  [149.5, 152.4], gap 27.7 ms with no overlap); N = 18 = 9,738 ms -> 7,389 ms (**-24.1 %**,
  CI 99.9 % [9,669, 9,807] -> [7,291, 7,487], gap 2,182 ms with no overlap). N = 18
  throughput moved from ~68 M to ~90 M solutions/sec on 28 logical cores. The larger win at
  N = 18 vs N = 16 matches the hypothesis: tail imbalance grows with subtree size, so the
  heavier the workload, the more headroom the dynamic partitioner recovers. Correctness
  preserved (424 / 424 unit tests green, including every All-mode count-only parallel gate:
  `BitboardNQueenSolverTests.CountSolutions_Parallel_*`,
  `LargeBoardAllSolutionCountsTests.AllMode_CountOnly_LargeBoards_Exact(n: 15)`,
  `HalfBoardFlagAllModeTests.CountOnly_AllMode_FlagOn_MatchesExpected(n: 15, 16, 17)`,
  `SolverParallelConsistencyTests.ParallelVsSequential_CountsMatch(n: 9..12, mode: All)`,
  `Slow.AllMode_CountOnly_LargeBoards_TotalMatches(n: 9..14)`,
  `BitmaskSolverAllModeTests.AllMode_CountOnly_N14_RoutesThroughBitboardCountSolutions`).
  Option B (a true `ConcurrentQueue<PartialState>` with depth-3 splitting and
  `LongRunning` Tasks) was designed as an escalation but is **not needed** for the current
  N range — Option A already cleared the ±1 % noise band at both measured board sizes.
  Files changed: `NQueen.Kernel/Solvers/BitboardNQueenSolver.cs` (the partitioner swap,
  ~5 lines + comment) and `NQueen.Benchmarking/AllModeBenchmarks.cs` (new
  `AllCountOnlyParallelScalingBenchmark`, `[Params(16, 18)]`, same job settings as the
  Unique guard).
- **Kernel performance — `BitmaskSolver.CountUniqueFastHalfBoard` Unique-mode count-only
  parallel dispatch switched to the same chunk-of-1 dynamic partitioner.** Same commit /
  same branch as the All-mode fix above. The half-board parallel DFS at
  `NQueen.Kernel/Solvers/BitmaskSolver.CountUnique.cs:90` previously fed its depth-2
  work-item array (built by `BuildUniqueDepth2WorkItems`, col-0 restricted to the top half)
  directly into `Parallel.ForEach(items, …)` with the default static range partitioner.
  Even though the canonical-prune gate (`EnablePartialReflectionPruning`) damps the
  first-queen variance somewhat, item-count-to-cores ratios at N = 16 / 17 are low enough
  that static slicing still leaves stragglers on 28 logical cores. The fix is one line —
  wrap the items array in `Partitioner.Create(items, EnumerablePartitionerOptions.NoBuffering)`
  so each worker pulls one item at a time. Same items, same `CountCanonicalDFS` recursion,
  same per-thread `Interlocked.Add` reduction — behaviour-identical. **A/B measurement
  (`UniqueFastHalfBoardEvenOddBenchmark`, full job: 3 warmups, 15 iterations):** N = 16 =
  248.9 ms -> 195.8 ms (**-21.3 %**, ±1.29 ms vs ±2.56 % baseline error, gap 53.1 ms with
  no CI overlap); N = 17 = 2,059.2 ms -> 1,419.5 ms (**-31.1 %**, ±6.50 ms vs ±0.53 %
  baseline error, gap 639.7 ms with no CI overlap). The N = 17 delta is in fact larger than
  the All-mode N = 18 delta — fewer total items relative to 28 logical cores makes the
  static-partitioner imbalance worse to start with, so the dynamic dispatcher recovers more.
  Same correctness gates green (513 / 513 across `NQueen.UnitTests` + `NQueen.ViewModelTests`,
  including all canonical-counting and reflection-pruning tests). The companion engine-path
  `Parallel.ForEach` in `NQueen.Kernel/Solvers/Engines/BitmaskParallelEngine.Unique.cs:52`
  is **deliberately untouched** — it is reachable only for `N < 16` count-only or for
  materialize/visualize paths, neither of which the regression-guard benchmark exercises.
  File changed: `NQueen.Kernel/Solvers/BitmaskSolver.CountUnique.cs` (~3 lines + comment).

### Docs
- **`investigate/unique-materialize-gap` — "Unique CountOnly vs Materialize gap"
  investigation (Step 3 from `docs/ROADMAP.md` Investigations) closed as
  gap-already-eliminated (no production-code changes shipped).** Branch opened
  off `main` (`37b8fdf`) to investigate the historical ~5–6× performance
  difference between Unique count-only and Unique materialize modes at
  N = 17–19, as noted in the ROADMAP Investigations backlog. Fresh baseline
  measurement via `UniqueHighNBenchmark` (ShortRun, 1 warmup / 3 iterations,
  N = 16–19) returned **CountOnly/Materialize ratios of 0.99–1.01×** (all within
  measurement noise; essentially identical performance). The historical gap was
  **fully eliminated** by the two-phase split in `EnumerateUniqueVisualizeAdaptive`
  shipped earlier (CHANGELOG.md lines 696–710): Phase 1 streams/animates up to
  the visualization cap (≤ 100 solutions by default), Phase 2 switches to
  count-only via `CountSolutions` to get the exact total without materialization
  overhead. The two-phase architecture correctly applies the same optimization to
  the Unique materialize path that was already present in the All-mode equivalent.
  Investigation concludes: no further action required; gap is eliminated by shipped
  code. No production changes; 535 / 535 tests stay green. Branch ships docs-only
  (ROADMAP.md updated to mark Step 3 as closed). Partial benchmark log archived as
  `NQueen.Benchmarking/baseline-unique-countonly-vs-materialize.log` for reference.
- **`perf/all-mode-iterative-search-bounds-elision` — "`Span<Frame>.get_Item`
  bounds-check elision in iterative `Search`" candidate (Step 2.4 of the perf execution
  queue, seeded from the Step 2.3 kill-check secondary discovery) closed as the **sixth
  profile-first negative finding in a row** (no production-code changes shipped).**
  Branch opened off freshly-merged `main` (post-PR #23, `f1552ac`) with the explicit
  scope of *deciding* whether replacing `stack[row - startRow]` reads/writes in
  `BitboardNQueenSolver.Search` (`NQueen.Kernel/Solvers/BitboardNQueenSolver.cs:199` and
  `:217`) with the canonical RyuJIT bounds-elision pattern — `ref Frame head = ref
  MemoryMarshal.GetReference(stack)` once + `Unsafe.Add(ref head, row - startRow)` for
  both the per-iteration backtrack-load and the push-store — would clear the > 1 %
  wall-clock decision gate at N = 18. Calibrated prior was **~50–70 % positive**
  (substantially higher than every previous candidate on this path) because the
  Step 2.3 (MRV heuristic) kill-check profile against
  `HalfBoardFlagAllModeTests.CountOnly_AllMode_FlagOn_MatchesExpected(n: 16)` had
  surfaced `Span<Frame>.get_Item` at **66.36 % Self [HOT]** — the **largest
  single-function Self attribution ever documented on this code path** and 2.6× the
  entire `Search` body's combined Self. Branch baseline
  (`option-A-all-mode-baseline.md`) re-established same-session at
  N = 16 = 139.7 ms ±0.40 %, N = 18 = 7,115.5 ms ±1.01 % — within ±1 % of the post-PR
  #20 iterative reference at both Ns. Decision gate fixed up front (mirroring PR #20):
  oracle parity at N ∈ [1, 14] across both `parallel: true` and `parallel: false` AND
  > 1 % wall-clock improvement at N = 18 with non-overlapping 99.9 % CIs AND
  non-regression at N = 16. **Variant A was implemented and measured** in this branch
  (~3 source lines: `using System.Runtime.InteropServices;` + the `MemoryMarshal.
  GetReference` ref + two `Unsafe.Add` rewrites). Oracle cleared cleanly: 535 / 535
  tests green including the 28 parity theories
  `BitboardNQueenSolverTests.CountSolutions_{Parallel,Sequential}_MatchesRecursive(n:
  1..14)`. **Same-session post-Variant-A measurement (same `AllCountOnlyParallelScaling
  Benchmark`, same job, same machine)**: N = 16 = 140.3 ms ±0.52 % (Δ +0.43 %, post CI
  [139.57, 141.03] ms overlapping baseline CI [139.14, 140.26] ms by 0.69 ms — within
  noise, non-regression OK), N = 18 = 7,071.1 ms ±0.47 % (Δ −0.62 %, post CI [7,037.90,
  7,104.30] ms overlapping baseline CI [7,043.89, 7,187.11] ms by 60.41 ms — **fails
  the > 1 % gate AND the non-overlapping-CI gate**). The point-estimate gain at N = 18
  (−44 ms) is smaller than the baseline run's own 99.9 % CI half-width (±71.6 ms), so
  it cannot be distinguished from run-to-run noise at the chosen confidence threshold.
  **Forensic disassembly evidence to settle *why* the gate failed**: captured the JIT-
  emitted assembly for `BitboardNQueenSolver.Search` in both forms via
  `DOTNET_JitDisasm=Search` on the same Release build, with tiered compilation off so
  only fully-optimized code emits. Baseline form (`stack[row - startRow]`) emits **two
  bounds-check sequences** in the inner loop — `cmp idx, length` + `jae G_M000_IG20` /
  `call CORINFO_HELP_RNGCHKFAIL` — one before the backtrack-load (`G_M000_IG08`,
  reading `frame.{Cols, D1, D2, Remaining}` from the stackalloc buffer) and one before
  the push-store (`G_M000_IG11`, writing the same fields). Variant A form
  (`Unsafe.Add(ref head, row - startRow)`) emits **zero bounds checks**: total `Search`
  body shrinks from 410 bytes to **383 bytes (−27 bytes, −6.6 %)**, the
  `CORINFO_HELP_RNGCHKFAIL` cold path block is eliminated entirely, and both inner-loop
  index computations drop directly into `shl idx, 5` + `add base` + the four `mov
  qword ptr` operations. **The bounds checks were real and Variant A successfully
  removed both** — yet the wall-clock did not move statistically. The structural lesson
  this closure ships: on this microarchitecture (Intel Core i7-14700K, X64 RyuJIT
  AVX2), the `cmp idx, length / jae cold` bounds-check pair is **essentially free at
  runtime**, despite costing instruction bytes — because the `jae` is virtually
  never-taken (out-of-bounds would be a bug), correctly-predicted by the static
  not-taken default; modern x86-64 cores fuse `cmp + jae` into a single front-end µop;
  and the pair runs in execution-port-parallel with the 4×qword frame loads, never
  sitting on the latency-critical chain. The 66.36 % Self the profiler attributed to
  `Span<Frame>.get_Item` was the **frame-load itself** — the four `qword ptr [base + 0
  / 8 / 16 / 24]` reads from stackalloc memory — which the line-attribution model
  folded into the indexer's symbol along with the elidable check; the profiler's
  symbol-level Self attribution cannot distinguish "bounds check fired" from "memory
  load occurred" on a tight loop body. **Sixth profile-first negative finding** on the
  All count-only `BitboardNQueenSolver.Search` code path; the first one where the
  surfaced symbol-level signal was strong enough on paper (66.36 % Self) to justify a
  measure-and-decide pass and the JIT actually emitted the predicted code-shape change,
  yet the runtime cost of the eliminated work was below the measurement floor.
  **Candidate-evaluation bar permanently raised** for this code path: future candidates
  on `BitboardNQueenSolver.Search` now require either (a) `[DisassemblyDiagnoser]` /
  `DOTNET_JitDisasm` evidence of the proposed change's **dynamic** cost difference, not
  symbol-level Self alone, or (b) µarch reasoning explaining why the change reduces
  critical-path latency rather than instruction count. Production code reverted to its
  pre-experiment state on the same branch (matches `main` at `f1552ac` for
  `BitboardNQueenSolver.cs` content); branch ships docs-only matching PR #16 / #17 /
  #19 / #23 precedents. No production-code changes; no new benchmark (the existing
  `AllCountOnlyParallelScalingBenchmark` and `AllCountOnlyRecursiveVsIterativeBenchmark`
  from PR #15 / PR #20 served as the permanent regression guards). The branch baseline
  (`option-A-all-mode-baseline.md`) and the two JIT-disasm captures
  (`jit-disasm-baseline-search-only.txt`, `jit-disasm-variant-A-search-only.txt`) stay
  in tree under `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/` as archived
  evidence (gitignored, but useful when bisecting future perf changes or evaluating
  bounds-elision-flavored candidates against the now-known noise floor). 535 / 535
  tests stay green (no production changes ship).
- **`perf/all-mode-mrv-heuristic` — "MRV (Minimum Remaining Values) heuristic for
  next-column branch ordering" candidate (from `Backlog → Larger wins, scoped risk`;
  Step 2.3 of the execution queue) closed as the **fifth profile-first negative finding
  in a row** (no production-code changes shipped).** Branch opened off freshly-merged
  `main` (post-PR #22, `d4b26cc`) with the explicit scope of *deciding* whether the
  iterative `BitboardNQueenSolver.Search` (the production hot path after PR #20's swap)
  should be modified to pick the next column whose `available = ~(cols | d1 | d2) & mask`
  has the fewest set bits — the most-constrained variable, classical CSP fail-fast —
  rather than walking columns 0..N-1 in fixed order. Two structural shapes were on the
  table: Variant A (column reorder fixed at root, cheap per-iteration), Variant B
  (dynamic per-level recompute, more aggressive pruning at the cost of per-level popcount
  work). Branch baseline `branch-baseline-all-mode-mrv-heuristic.md` re-established at
  N = 16 = 140.4 ms ±0.66 %, N = 18 = 7,043.0 ms ±0.44 % — both within ±1 % of the
  post-PR #20 iterative reference, confirming the prior session's environmental
  regression suspicion was a warm-up / scheduler artifact, not a code or toolchain
  regression. Decision gate established up front (mirroring the iterative-core branch):
  oracle parity at N ∈ [1, 14] across both `parallel: true` and `parallel: false` AND
  > 1 % wall-clock improvement at N = 18 with non-overlapping 99.9 % CIs AND
  non-regression at N = 16. Prior probability calibrated low (~10–20 % positive) because
  `Search` runs at register-tight bit-mask ops and MRV adds per-level popcount work
  *onto* the hot path. **The kill signal arrived from line-level CPU attribution before
  any production change** via `profile_unit_test` against
  `HalfBoardFlagAllModeTests.CountOnly_AllMode_FlagOn_MatchesExpected(n: 16)` — the
  production All-mode count-only dispatch (`BitmaskSolver.GetSimResultsAsync` →
  `EnumerateAllAdaptive(countOnly: true)` → `BitboardNQueenSolver.CountSolutions` →
  `Parallel.ForEach` over depth-2 work items → iterative `Search`). **Function-level
  rollup**: `BitboardNQueenSolver.Search` body 91.84 % Total / **25.48 % Self**, and
  `Span<Frame>.get_Item(int)` (the bounds-checked frame-load on backtrack) at **66.36 %
  Self [HOT]**. The 25.48 % `Search` body Self is the *combined* attribution for **all
  eight inline ops in the loop body** (`if (available == 0)` + backtrack pop, `bit =
  available & -available`, `available &= available - 1`, leaf-shortcut, frame push,
  diagonal-shift descend block, `row++`, and the per-row `available = ~(cols | d1 | d2)
  & mask` line MRV would manipulate). Eight inline ops sharing 25.48 % Self gives a
  per-op average around 3.2 % at most, with a heavily uneven actual distribution — and
  even on the most generous read, **no single inline op including the `available = …`
  line plausibly clears 2–3 % Self attributable specifically to it**. The line folds
  into the bit-scan loop's per-iteration Self exactly the way the diagonal shifts
  `(d1 | bit) << 1` and `(d2 | bit) >> 1` did on `perf/cached-diagonal-shifts`
  (PR #16) — which is precisely the failure mode the kill criterion was written to
  detect. Beyond the line-attribution question, the dominant cost (`Span<Frame>.get_Item`
  at 66.36 % Self, **2.6× the entire `Search` body's combined Self**) is cleanly outside
  MRV's lever entirely: MRV changes which column is branched next, but does not reduce
  the number of frames pushed onto the stack along the path to a solution (N queens
  still require N push/pop pairs along any successful branch). MRV would *add*
  per-level popcount work onto `Search`'s body — `(N - d)` popcount calls plus a
  min-reduction at depth `d`, 4–8 popcounts per descent at N = 16, each touching state
  that would otherwise stay in registers. The pruning savings would have to dominate
  that added ALU + register-pressure cost, but the line evidence above shows the
  bit-mask ops MRV's pruning would amortize against are *already* small — the dominant
  cost lives in the Span backtrack-load that MRV does not touch. The pattern reasserts:
  positives on this path require either *removing a per-leaf operation entirely*
  (PR #20's leaf-shortcut) or *re-targeting a structural inefficiency off the hot path*
  (PR #15's chunk-of-1 partitioner moved tail-imbalance work *off* `Search`); adding
  work *on* the hot path — even with plausible pruning upside — has now failed twice in
  identical fashion (this branch and PR #16). **Secondary discovery (out of scope but
  recorded for the perf backlog)**: `Span<Frame>.get_Item` at 66.36 % Self is a
  genuinely new signal on this code path. PR #20 swapped recursive→iterative and won
  −3.0 % at N = 18 / −3.1 % at N = 16; the post-PR #20 profile now shows the
  *replacement* mechanism (stackalloc Span<Frame> with bounds-checked indexing) is
  itself the dominant runtime cost, not an incidental one. A backlog entry seeding this
  hypothesis (likely `ref Frame f = ref MemoryMarshal.GetReference(stack)` +
  `Unsafe.Add` to elide the bounds check, with its own MEASURE-first correctness gates)
  is added to `docs/ROADMAP.md → Backlog → Kernel Performance → Larger wins, scoped
  risk` so it can be picked up cleanly on a future branch. Acting on it inside this
  branch was deliberately not done — the kill criterion was written specifically to
  evaluate MRV; conflating two experiments would undermine the perf-discipline pattern
  (one branch = one MEASURE-first hypothesis). No production-code changes; no new
  benchmark (the existing `AllCountOnlyParallelScalingBenchmark` and
  `AllCountOnlyRecursiveVsIterativeBenchmark` from PR #15 / PR #20 already serve as
  permanent regression guards for this code path). The branch baseline
  (`branch-baseline-all-mode-mrv-heuristic.md`) and the kill-check evidence doc
  (`kill-check-mrv-heuristic.md`) both stay in tree under
  `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/` as archived evidence
  (gitignored, but useful when bisecting future perf changes). 535 / 535 tests stay
  green (no production changes). Branch ships docs-only.
- **`perf/all-mode-symmetry-reduction` — "Symmetry reduction in All count-only path"
  candidate (from `Backlog → Larger wins, scoped risk`) closed as the **fourth
  profile-first negative finding in a row** (no production-code changes shipped).**
  Branch opened off freshly-merged `main` (post-PR #18, `7d07a69`) with the explicit
  scope of *deciding* whether the remaining D4 factor of up to 4× — beyond the existing
  half-board reflection captured by `BitboardNQueenSolver.CountSolutions` (`row0 ∈
  [0, N/2)` + `count *= 2`, with center-row handling for odd N) — could be extracted via
  a port of the reflection-only forward-prefix prune `SearchHelpers.ShouldPrunePrefixFull`
  from the Unique path. Branch baseline `branch-baseline-all-mode-symmetry-reduction.md`
  re-established two cross-checking measurements on `7d07a69`:
  `AllCountOnlyN18Benchmark` N = 18 ≈ 7,403 ms ±0.67 %, and
  `AllCountOnlyParallelScalingBenchmark` N = 16 ≈ 148.1 ms ±0.78 %, N = 18 ≈ 7,358.8 ms
  ±0.77 % (cross-benchmark agreement at N = 18: 0.6 % apart, well inside run-to-run
  drift; N = 16 = 148.1 ms consistent with the post-PR-#15 expected band of 151.0 ms).
  Decision gate established up front: oracle gate (counts match
  `ExpectedSolutionCounts.AllSolutions` for all N ∈ [4, 18]) — non-negotiable; perf gate
  (±1 % wall-clock improvement at N = 18) + non-regression at N = 16 — must both clear
  to ship. **The kill signal arrived from code reading before any production change**:
  `SearchHelpers.ShouldPrunePrefixFull` (`SearchHelpers.cs:73-84`) is a stateless
  reflection-only forward-prefix prune that walks `for i in [0..depth]: if rows[i] >
  N-1-rows[i] return true; if rows[i] < N-1-rows[i] break`. The existing All count-only
  path already restricts row 0 to the top half (`BitboardNQueenSolver.cs:80`,
  `for (int row0 = 0; row0 < half; row0++)`); for any `row0 ∈ [0, N/2)` on **even N**,
  `row0 < N-1-row0` strictly — so the prune's loop hits `break` at `i = 0` and returns
  `false` for every node in the tree. The benchmark targets are N = 16 and N = 18 —
  both even — so porting the prune would fire **zero times** at the measured sizes.
  Added per-node work would be pure overhead with zero pruning benefit, guaranteeing
  regression at exactly the sizes the decision gate measures. The structural argument
  closes the entire candidate, not just the reflection-prune sub-design: the half-board
  restriction `row0 ∈ [0, N/2)` on even N already captures the maximal subgroup of the
  row-reflection prune; the remaining D4 factor of up to 4× lives in the rotation
  symmetries (rot90, rot180, rot270), which are **not column-preserving** and therefore
  not amenable to forward-prefix pruning at all — they require leaf-level canonical-form
  checking, which on a path that runs at 99.99 % Self CPU on register-tight bit-mask
  operations (per the PR #17 trace evidence) is pure overhead. A quarter-board
  fundamental-domain enumeration with closed-form orbit weighting (Option B) would have
  bounded upside (≈25-40 % wall-clock at best), non-trivial implementation
  (≈200-300 lines + extensive correctness validation against the N ≤ 18 oracle), and
  a ≈75 % prior probability of becoming the negative finding regardless given the three
  preceding negatives — not pursued. No production-code changes; no new measurement
  artifact (the existing `AllCountOnlyN18Benchmark` and `AllCountOnlyParallelScalingBenchmark`
  established by PR #15 already serve as permanent regression guards for this code path).
  Branch ships docs-only.
- **`perf/all-mode-arraypool` — `ArrayPool<T>` for column / diagonal / row stacks on the
  All-mode materialize path (from `Backlog → Larger wins, scoped risk`) profile-first
  investigation closed with a negative finding (no production-code changes shipped).**
  Third profile-first closure in a row, following the same pattern as queues #1
  (`perf/unique-iterative-core`) and #2 (`perf/cached-diagonal-shifts`). Branch opened
  off freshly-merged `main` (post-PR #16) with the explicit scope of *deciding* whether
  pooling the per-call `int[]` / `Frame[]` buffers in `BitmaskSearchEngine.CreateState`
  (`NQueen.Kernel/Solvers/BitmaskSearchEngine.cs:62-87`) and the per-solution `int[]`
  copies in `BitmaskSolver.All.cs` (the `new int[rowsFound.Length]` at line 48 of
  `RunAllUnified` and the `new int[N]` at line 119 of `CollectAllSampleSolutionsDFS`)
  via `ArrayPool<T>.Shared` would reduce GC pressure at N ≥ 18, **before** writing the
  pool plumbing. Three independent measurement attempts agreed on the negative branch:

  1. **`profile_unit_test` MEMORY at N = 14**
     (`BitmaskSolverAllModeTests.AllMode_Materialize_N14_RoutesThroughTwoPhasePath`) —
     the top-3 allocation types were all xUnit reflection plumbing
     (`System.String` 25,592 allocs / 2.20 MB, `System.Reflection.CustomAttributeType`
     23,955 allocs / 958 KB, `System.Reflection.CustomAttributeNamedParameter` 19,613
     allocs / 941 KB), with `createdBy` chains entirely in `System.Reflection.*` and
     `System.Text.StringBuilder`. **No user-code type cracked the top-3** — the All-mode
     materialize allocation surface was below the test-runner noise floor at N = 14.
     Trace preserved as `documentSessionId d27e4774-6318-4f84-8f46-4a8c486e775e`.
  2. **`run_benchmark` MEMORY against the existing
     `AllModeVariantsBenchmark.All_Sequential_Materialize`** at N = 12 and N = 15 —
     returned **timing-only output** (no `Gen0` / `Gen1` / `Gen2` / `Allocated` columns).
     The existing class does not carry `[MemoryDiagnoser]`, and per the established
     measurement-artifact-editing discipline (see
     `docs/ROADMAP.md → Process (rule)`) production benchmarks are not retroactively
     decorated. Wall-clock at N = 15 was ≈ 414 ms ±1.4 % across all four
     (`SplitDepth` × `EnablePruning`) cells — showing the materialize path is
     **insensitive** to the existing pruning / split-depth knobs, consistent with a
     workload dominated by the `MaxDisplayedCount`-capped phase-1 sample emission plus
     the phase-2 count-only DFS, not by search pruning.
  3. **`run_benchmark` MEMORY against a new purpose-built
     `AllModeMaterializeAllocationBenchmark`** — created on this branch with
     `[MemoryDiagnoser]`, `[Params(15, 18)]`, and the same job settings
     (3 warmups, 15 iterations) as `UniqueFastHalfBoardEvenOddBenchmark` and
     `AllCountOnlyParallelScalingBenchmark`. Measured **N = 15 ≈ 82.40 ms ±1.40 %,
     N = 18 ≈ 7,420.97 ms ±0.49 %** on the dev machine (i7-14700K, .NET 10.0.8),
     preserved as
     `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/branch-baseline-all-mode-arraypool.md`.
     The `[MemoryDiagnoser]` **also emitted no allocation columns** at either size —
     when present, those columns are always reported, so their absence means per-op
     allocations are below BenchmarkDotNet's reporting threshold (typically < ~1 KB).
     The supplementary CPU trace included with the benchmark run
     (`documentSessionId 93efc046-ed82-4ac6-a8e0-a439f215d463`) was **decisive**:
     `BitboardNQueenSolver.Search(int, ulong, int, ulong, ulong, ulong)` at
     **99.99 % Total / 99.99 % Self**, with the recursion shape rising from 0.43 % Self
     at the outermost frame to 38.16 % / 28.11 % at depths 6–7. Source inspection
     confirmed `BitboardNQueenSolver.Search`
     (`NQueen.Kernel/Solvers/BitboardNQueenSolver.cs:95–122`) takes only `ulong` / `int`
     arguments, performs zero managed allocations, holds zero stack arrays, and uses
     zero `new` operators inside the recursion (the `// Allocation-free hot path`
     comment in the source is accurate).

  The decision gate established on the branch
  (`branch-baseline-all-mode-arraypool.md`: allocation hotspot must surface AND
  wall-clock A/B must clear ±1 % at N = 18 → ship; otherwise abandon) returned the
  **negative branch**. The phase-1 sample DFS (`CollectAllSampleSolutionsDFS`) does
  allocate a single `int[N]` per call and `int[N]` per solution copy at N > 25, but
  terminates within milliseconds (capped at `SimulationSettings.MaxDisplayedCount`
  samples) and is invisible against the 99.99 % Self of the phase-2 `Search`.
  Similarly, `BitmaskSearchEngine.CreateState` allocations (`int[N]` queen rows,
  `Frame[N]` stack, `int[N]` solution buffer) belong to `RunAllUnified`, which
  `EnumerateAllAdaptive(countOnly: false)` deliberately skips at N ≥ 14 — they are
  **never reached** on the path the ROADMAP candidate targets. **Outcome:** the
  `ArrayPool<T>` candidate is abandoned, `perf/all-mode-arraypool` ships docs-only plus
  the new `AllModeMaterializeAllocationBenchmark` as a **permanent regression guard**
  for the All-mode materialize allocation surface (durable irrespective of the
  experiment's outcome — it will catch any future regression on this path). The next
  perf branch picks from `docs/ROADMAP.md → Backlog → Kernel Performance → Larger
  wins, scoped risk`; the remaining candidates are **Symmetry reduction in All
  count-only**, **Iterative core for All mode**, and **MRV heuristic**. Files changed:
  `docs/ROADMAP.md` (Next session, Active branch row, Recently shipped, Larger wins
  preamble + ArrayPool bullet struck through, the shipped depth-based work-stealing
  bullet removed per the document convention), `CHANGELOG.md` (this entry),
  `NQueen.Benchmarking/AllModeMaterializeAllocationBenchmark.cs` (new, ~50 lines), and
  `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/branch-baseline-all-mode-arraypool.md`
  (new baseline document).
- **`perf/cached-diagonal-shifts` — Candidate queue #2 profile-first investigation closed
  with a negative finding (no production-code changes shipped).** Branch opened off
  freshly-merged `main` (post-PR #15) with the explicit scope of *deciding* whether
  `(d1|bit)<<1` / `(d2|bit)>>1` in `CountCanonicalDFS`
  (`NQueen.Kernel/Solvers/BitmaskSolver.CountUnique.cs:207`) should be replaced with a
  per-row cached shifted-mask table, **before** writing the cache. Re-ran
  `UniqueFastHalfBoardEvenOddBenchmark` under the full job (3 warmups, 15 iterations) on
  this `main` build to re-establish a baseline — **N = 16 ≈ 195.7 ms ±0.63 %, N = 17 ≈
  1,416.3 ms ±0.53 %** on the dev machine (i7-14700K, .NET 10.0.8), preserved as
  `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/branch-baseline-cached-diagonal-shifts.md`.
  Both values agree with the PR #15 post-merge measurement to within −0.05 % / −0.22 %,
  well inside the ±1 % noise band. Line-level attribution was then collected via
  `profile_unit_test` against
  `BitmaskSolverCountUniqueTests.CountUniqueAdaptive_PreservesPruningFlags(n: 16, initialFlags: False)`
  (the N = 16 case routes through `CountUniqueFastHalfBoard` → `CountCanonicalDFS`, the
  same hook that produced the queue-#1 negative finding on `perf/unique-iterative-core`).
  Result: **91.78 % Total inside the `Parallel.ForEach` body → recursive
  `CountCanonicalDFS`**, with per-frame Self% peaking at 14.66 % (col 5), 13.85 %
  (col 6), 12.05 % (col 7) and tapering as the recursion unwinds (col 4 = 9.15 %, col 3
  = 4.87 %, col 2 = 1.96 %). The only non-trivial leaf in the trace was
  `BitOperations.TrailingZeroCount` at **5.17 % Self** (out-of-line despite its
  `[AggressiveInlining]` attribute, matching the queue-#1 trace). **The `(d1|bit)<<1`
  and `(d2|bit)>>1` expressions on `BitmaskSolver.CountUnique.cs:207` did NOT surface as
  a separate sample band** — they fold into the recursive `CountCanonicalDFS` per-frame
  Self% alongside the bit-scan loop body (`avail ^= bit`, `rows[col] = r`, the prune-gate
  branch). The decision gate established on the branch (≥ 2–3 % Self attributable
  specifically to the shifts → write the cache; < 1 % or folded into the bit-scan loop
  → document negative finding and abandon) returned the **negative branch**. This
  empirically confirms the skeptical prior recorded in the original Candidate queue #2
  entry: because `bit = avail & -avail` is recomputed inside the loop and unique per
  iteration, a per-iteration "cache" of `(d1|bit)<<1` / `(d2|bit)>>1` cannot amortise
  across iterations — it would only rename two ALU ops that the JIT has already compiled
  into the bit-scan loop's per-frame Self time. **Outcome:** the per-row
  shifted-diagonal-mask table is abandoned, `perf/cached-diagonal-shifts` ships docs-only,
  and the next perf branch picks from `docs/ROADMAP.md → Backlog → Kernel Performance →
  Larger wins, scoped risk` (the original Candidate queue is now exhausted: queue #1
  abandoned, queue #2 abandoned, queue #3 shipped as PR #15). ROADMAP "Next session",
  "Candidate queue #2", "Current State → Active branch", "Recently shipped", and the
  "Backlog → Larger wins" preamble + bullet are updated to match.
- **`perf/all-work-stealing` opened off `main` (`f75c5ea`) — Candidate queue #3: depth-based work-stealing queue for All mode.** Branch targets the documented tail-imbalance at large N on >8 cores with the current root-only `Parallel.ForEach` scheduling (source: `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20`). ROADMAP "Next session", "Candidate queue #3", and "Current State → Active branch" updated to reflect the new active branch.
- **`perf/unique-iterative-core` — Option C profile-first investigation closed with a
  negative finding (no production-code changes shipped).** Branch was opened off
  freshly-merged `main` (`f75c5ea`) with the explicit scope of *deciding* whether
  `CountCanonicalDFS` (`NQueen.Kernel/Solvers/BitmaskSolver.CountUnique.cs:165-209`)
  should be ported to an iterative DFS, **before** writing the port. Re-ran
  `UniqueFastHalfBoardEvenOddBenchmark` under the full job (3 warmups, 15 iterations) on
  this `main` build to re-establish a baseline — **N=16 ≈ 254.8 ms ±1.25 %, N=17 ≈
  2,103.0 ms ±0.93 %** on the dev machine (i7-14700K, .NET 10.0.8); the pre-event-migration
  reference of **244 ms / 2,042 ms** is retired in favour of these figures (the ~3–4 % delta
  is within natural cross-build drift). The same benchmark was re-run with
  `[CPUUsageDiagnoser]` to drop a CPU-sampling ETL at
  `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/BenchmarkDotNet_UniqueFastHalfBoardEvenOddBenchmark_20260608_213732/1804E697-BC82-40D3-95F7-7D72D3B9E9D5/sc.user_aux.etl(x)`.
  `analyze_perf_trace` returned no findings against the raw ETL, so the line-level
  attribution was completed via `profile_unit_test` against
  `BitmaskSolverCountUniqueTests.CountUniqueAdaptive_PreservesPruningFlags(n: 16, …)`.
  Result: ~90 % Total inside the `Parallel.ForEach` body → `CountCanonicalDFS` recursion,
  with the deepest two frames (cols 13–14) carrying 16–19 % Self each, cols 10–12
  carrying 6–17 % Self each. **No call/ret/prologue/epilogue bucket appeared as a
  separate sample group** — the original ≥ 8 % gate cannot fire, so the iterative-DFS
  hypothesis is empirically refuted. The only non-trivial leaf in the trace was
  `BitOperations.TrailingZeroCount` at **5.01 % Self** (out-of-line despite its
  `[AggressiveInlining]` attribute); a one-line pre-experiment (Option C-2) routed it
  through a hand-inlined `Bmi1.X64.TrailingZeroCount` helper. The re-profile confirmed
  RyuJIT inlined the helper and the leaf physically vanished, but **wall-clock did not
  move** — N = 16 = 256.557 ms ±1.11 % vs baseline 254.8 ms ±1.25 % (+0.7 %, fully inside
  both noise bands). The 5 % attribution was sampling noise around the call site, not
  recoverable wall-clock work, so the C-2 change was reverted. **Outcome:** the
  iterative-DFS port is abandoned, `perf/unique-iterative-core` ships docs-only, and the
  next perf branch picks up Candidate queue #3 (**work-stealing for All mode**, source
  `docs/ignored/Archive/Potential All Mode Improvements.txt:1-20`). ROADMAP "Plan of
  work", "Current State → Active branch", and "Candidate queue" are updated to match.
- **Stage 6 docs sweep — `README.md` Solver-Options preface and `docs/ROADMAP.md` backlog
  refreshed for the post-migration cancellation surface.** README "Solver Options" preface
  now points at the post-migration entry point — properties on `BitmaskSolver` are set before
  calling `GetSimResultsAsync(SimulationContext)` (not the legacy `Solve()`), with per-call
  sinks and a `CancellationToken` flowing through `SimulationContext`; the property table is
  unchanged. ROADMAP *Backlog → Small wins, low risk* drops the stale "Throttle
  `IsSolverCanceled` reads" entry — the field was deleted in Stage 6 and the equivalent
  throttle on the cancellation-token poll (`(col & 0xF) == 0 && IsCancellationRequested` in
  `BitmaskSolver.CountUnique.cs::CountCanonicalDFS`) is already in place, so the subsection
  is now empty by intent rather than by oversight.
- **Post-event-migration sweep — `README.md` + `.github/copilot-instructions.md` updated.**
  Refreshed the now-stale terminology that referenced the deleted event surface: the project-tree
  caption in both files calls the Domain types `context records` (not `event-args`), the
  `copilot-instructions.md` types-bullet documents the three sink payload records
  (`ProgressInfo`, `SolutionFoundInfo`, `QueenPlacedInfo` in `NQueen.Domain.Context`) instead of
  the deleted `*EventArgs` types in the deleted `NQueen.Domain.EventArgs`, and the
  *Interface Conventions* block describes the post-Stage-6 surface — `ISolverFrontEnd` exposes
  only `DelayInMillisec` + `ProgressValue`; `ISolverBackEnd` exposes `UseCountOnlyAllMode` /
  `UseCountOnlyUniqueMode` / `GetSimResultsAsync(SimulationContext)`; notifications and
  cancellation flow per-call via `SimulationContext` (`IProgress<ProgressInfo>`,
  `IProgress<SolutionFoundInfo>` synchronous via `SynchronousProgress<T>`,
  `ChannelWriter<QueenPlacedInfo>` (conflating bounded channel with `DropOldest`), and a
  `CancellationToken`). Also fixed five U+FFFD replacement-character glyphs in
  `copilot-instructions.md` (left over from a prior bad encoding round-trip) — em-dashes and
  smart quotes are now the intended Unicode characters.
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
- **Event migration §1a pre-work audit — finding: _preventive_, not corrective (no live leak).**
  Audited whether the existing manual lapsed-listener mitigation removes the leak correctly and
  completely (per `EVENT-MIGRATION-PLAN.md` §1a) before starting the migration. A workspace-wide
  search found **exactly six** subscription sites for the three solver events — all in
  `MainViewModel.Events.cs` (`+=` lines 38–40, matching `-=` lines 46–48) — and **zero lambda/
  `delegate` subscriptions** (the silent-leak pattern). The mitigation is sound on every axis:
  symmetric named-method subscribe/unsubscribe; an idempotent `SubscribeToSimulationEvents()` that
  unsubscribes first (cannot accumulate duplicates); a per-run subscribe/unsubscribe cycle driven
  by `ManageSimulationStatus(Started/Finished)`; and a disposal chain
  (`MainWindow.OnClosing` → `MainViewModel.Dispose()` → `UnsubscribeFromSimulationEvents()`). The
  DI lifetime graph (singleton `MainWindow` → transient `MainViewModel` → transient `ISolver`,
  each resolved exactly once) means the solver never outlives the VM, so the lapsed-listener
  condition is unreachable. **Conclusion:** the migration is *preventive* — its value is making the
  no-leak guarantee **structural** (impossible to reintroduce if DI lifetimes later change to
  per-run or multi-window), not fixing a live defect.

### Changed (NQueen.Kernel)
- **Event migration Stage 0 — notification-seam extraction (behaviour-preserving).** Collapsed the
  ~25 inlined solver event-raise sites scattered across `BitmaskSolver.Single.cs` / `.Unique.cs` /
  `.All.cs` / `.Materialize.cs` / `.cs` behind three private `[MethodImpl(AggressiveInlining)]`
  helpers on `BitmaskSolver` — `RaiseProgress(double)`, `RaiseQueenPlaced(Memory<int>, int)`,
  `RaiseSolutionFound(int[], int)` — so every notification now flows through a single chokepoint
  (the only three `event?.Invoke(...)` calls left in the kernel). This turns each later
  sink-migration stage into a one-helper-body change instead of a multi-file raise-site hunt.
- **Resolved the `EnableEvents` gate inconsistency (the one deliberate behaviour change).** The
  terminal `100.0` progress raises in `.Single.cs` / `.Unique.cs` / `.All.cs` / `.Materialize.cs`
  were previously **ungated** while the intermediate raises were gated, so a consumer with
  `EnableEvents = false` still received one final 100% callback. The gate now lives once in
  `RaiseProgress` and applies **uniformly**: a disabled progress sink reports nothing, terminal
  100% included. Harmless for current consumers (the GUI runs `EnableEvents = true` and finalises
  its progress bar via `ManageSimulationStatus(Finished)`; headless/count-only paths ignore
  progress), and verified by the full kernel + view-model suite (84 affected tests green,
  including the `*VisualizePath_FiresQueenPlacedAndSolutionFoundEvents` and progress-heartbeat
  tests).

### Changed (Event migration — Stage 1)
- **Progress → `IProgress<ProgressInfo>`; the `Guid` simulation token is deleted.** The solver's
  `ProgressValueChanged` event and its run-correlation plumbing are replaced by a per-call push
  sink carried on `SimulationContext`:
  - **`NQueen.Domain`** — new `ProgressInfo(double Percent)` record struct (in
    `NQueen.Domain.Context`); `SimulationContext` gains an optional
    `IProgress<ProgressInfo>? OnProgress = null` parameter (null = "don't report", replacing the
    `EnableEvents = false` idiom for progress); `ISolverFrontEnd` drops
    `event ... ProgressValueChanged` and `SetSimulationToken(Guid)`; the now-orphaned
    `ProgressUpdateEventArgs` is removed.
  - **`NQueen.Kernel`** — `BitmaskSolver` captures `simContext.OnProgress` into a private
    `_onProgress` field in `GetSimResultsAsync`; `RaiseProgress` now forwards to
    `_onProgress?.Report(new ProgressInfo(percent))` (still gated by `EnableEvents`). The
    `_currentSimToken` field, `SetSimulationToken`, the event, and its `Dispose` null-out are
    gone. The dead `Guid SimulationToken` parameter is also removed from the internal
    `BitmaskSearchEngine.Request` record and its five construction sites (it was never read).
  - **`NQueen.GUI`** — `SimulateAsync` builds a fresh `new Progress<ProgressInfo>(OnProgressReported)`
    per run and passes it via `SimulationContext`; the `_currentSimulationToken` field, the
    `SetSimulationToken` call, the `Guid.Empty` completion guard, and the token reset in `Cancel`
    are deleted. The former `OnProgressValueChangedEvent` handler becomes `OnProgressReported`
    (the token guard disappears; the `IsSolverCanceled` guard and the Single+Visualize
    progress-visibility special-case are preserved).
- **Run correlation is structurally unnecessary now.** Because a new `Progress<T>` sink is created
  per `SimulateAsync` call, a previous run's callbacks can no longer reach the current view-model —
  the Guid token that the old `OnProgressValueChangedEvent` guard compared against is obsolete.
  Verified by the full suite (**513 / 513 green**), including the `ProgressBar_ShouldUpdate` test
  re-pointed to drive the `OnProgress` sink and the `ProgressRelayTests` heartbeat.

### Changed (Event migration — Stage 2)
- **Cancellation → `CancellationToken` threaded through `SimulationContext`.** The view-model's
  `CancellationTokenSource` was previously vestigial (cancellation reached the kernel only via the
  `IsSolverCanceled` bool). It now carries a real signal end-to-end:
  - **`NQueen.Domain`** — `SimulationContext` gains an optional `CancellationToken Cancellation = default`
    parameter (a default token is never "cancellation requested", so direct `Solve()` callers and
    3-/4-arg construction sites are unaffected).
  - **`NQueen.Kernel`** — `BitmaskSolver` captures `simContext.Cancellation` into a private
    `_cancellation` field in `GetSimResultsAsync` and exposes a single internal
    `IsCancellationRequested => IsSolverCanceled || _cancellation.IsCancellationRequested`. Every
    hot-loop cancellation read and every engine `IsCanceled` callback in `.All.cs` / `.Single.cs` /
    `.Unique.cs` / `.CountUnique.cs` now reads through that one property (the `(col & 0xF) == 0`
    throttle on the count-unique read is preserved).
  - **`NQueen.GUI`** — `SimulateAsync` captures `CancellationTokenSource.Token` up front (so a
    `Cancel()` that disposes/recreates the CTS cannot swap the source mid-run) and passes it via
    `SimulationContext`.
- **`IsSolverCanceled` is kept as a thin shim this stage** (per the migration plan): the kernel
  honours both the bool and the token by OR-ing them, so the existing in-flight cancellation tests
  (which toggle the bool from event callbacks) stay green. The bool, the explicit
  `CancellationTokenSource` lifecycle, and the VM cancel guards collapse onto the
  `IAsyncRelayCommand` token as the single source of truth in **Stage 5**. Verified by the full
  suite (**513 / 513 green**).

### Changed (Event migration — Stage 3)
- **`SolutionFound` → `IProgress<SolutionFoundInfo>` (synchronous, dual-emit).** The view-model now
  receives materialised solutions through a per-call sink on `SimulationContext` instead of the
  `SolutionFound` event:
  - **`NQueen.Domain`** — new `SolutionFoundInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical)`
    record struct (in `NQueen.Domain.Context`); `SimulationContext` gains an optional
    `IProgress<SolutionFoundInfo>? OnSolutionFound = null` parameter.
  - **`NQueen.GUI`** — new `SynchronousProgress<T>` adapter whose `Report` runs the handler
    **inline on the calling thread** (unlike `System.Progress<T>`, which posts asynchronously).
    This is required because the solver reports from a reused depth-first-search buffer that is
    overwritten as the search continues, so the handler must copy the payload (`Memory<int>.ToArray()`)
    before control returns to the solver — exactly the synchronous semantics the event had. The
    former `OnSolutionFoundEvent` becomes `OnSolutionFoundReported(SolutionFoundInfo)` with an
    identical body (synchronous batch add; `ObservableSolutions` mutations still marshalled through
    `IDispatcher`). `SimulateAsync` builds the sink and passes it via `SimulationContext`; the VM no
    longer subscribes to `SolutionFound`.
  - **`NQueen.Kernel`** — `BitmaskSolver` captures `simContext.OnSolutionFound` into a private
    `_onSolutionFound` field; `RaiseSolutionFound` now **dual-emits** — it still raises the
    `SolutionFound` event *and* reports to the sink, under the same
    `EnableEvents && !_eventsSuppressedAfterCap` gate.
- **The event is retained as a thin shim this stage** (per the migration plan): five kernel unit
  tests subscribe to `BitmaskSolver.SolutionFound` directly, so dual-emit keeps them green; the
  event and `SubscribeToSimulationEvents` plumbing are deleted in **Stage 5**.

### Changed (Event migration — Stage 4)
- **`QueenPlaced` → conflating `Channel<QueenPlacedInfo>` (drop-oldest, dual-emit).** The
  high-frequency partial-prefix stream that drives board animation now flows through a bounded,
  keep-latest channel drained by the existing visualization `DispatcherTimer`, instead of the
  `QueenPlaced` event. At large N with zero delay the solver can fire this notification extremely
  fast; an `IProgress<T>.Report` per placement would `Post` to the dispatcher on every call and
  flood it, so this one stream is modelled as a channel while progress and solutions stay on
  `IProgress<T>`:
  - **`NQueen.Domain`** — new `QueenPlacedInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical)`
    record struct (in `NQueen.Domain.Context`); `SimulationContext` gains an optional
    `ChannelWriter<QueenPlacedInfo>? OnQueenPlaced = null` parameter (null = "don't animate", used by
    Hide / count-only runs). `System.Threading.Channels` is added to the Domain/Kernel/GUI global
    usings (it is not part of implicit usings).
  - **`NQueen.Kernel`** — `BitmaskSolver` captures `simContext.OnQueenPlaced` into a private
    `_onQueenPlaced` field; `RaiseQueenPlaced` now **dual-emits** — it still raises the `QueenPlaced`
    event *and* `TryWrite`s a `QueenPlacedInfo` to the channel, under the same
    `EnableEvents && !_eventsSuppressedAfterCap` gate. Because the channel drain is deferred to the
    UI timer, the prefix is **copied** (`rows.ToArray()`) before the write; the `_onQueenPlaced?.`
    null-conditional short-circuits that copy entirely when no channel is wired (Hide / benchmark /
    headless), so those paths pay zero extra cost.
  - **`NQueen.GUI`** — `SimulateAsync` creates the channel only in Visualize mode via
    `Channel.CreateBounded<QueenPlacedInfo>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest })`,
    passes `channel.Writer` through `SimulationContext`, starts the drain (`StartQueenPlacedDrain`),
    and completes the writer in `finally`. The former `OnQueenPlacedEvent` handler is replaced by
    `DrainQueenPlacedChannel`, invoked from each `VisualizationTimer_Tick` on the UI thread: it reads
    to the most recent prefix (keep-latest), stages it into `_pendingPrefixRows`/`_pendingDepth`, and
    the render logic preserves both animation paths — **one column per tick** when a delay is set and
    the **full latest prefix** at zero delay (the timer throttles to ~1 ms). The VM no longer
    subscribes to `QueenPlaced`; `_uiDispatcher.Invoke` marshalling is no longer needed since the
    drain already runs on the dispatcher.
- **The event is retained as a thin shim this stage** (per the migration plan): three kernel/
  view-model tests subscribe to `BitmaskSolver.QueenPlaced` directly, so dual-emit keeps them green;
  the event and the visualization-timer subscription plumbing are deleted in **Stage 5**.

### Changed (Event migration — Stage 5)
- **The solver event scaffolding is removed.** With every consumer migrated to push sinks in
  Stages 1–4, the two remaining notification `event`s and their payload types are deleted:
  - **`NQueen.Domain`** — `ISolverFrontEnd` drops `event ... QueenPlaced` and
    `event ... SolutionFound` (it now exposes only `DelayInMillisec` / `ProgressValue`); the
    `QueenPlacedEventArgs` and `SolutionFoundEventArgs` types are deleted along with the now-dead
    `global using NQueen.Domain.EventArgs;` in Domain / Kernel / GUI / ViewModelTests. The
    `QueenPlacedInfo` / `SolutionFoundInfo` XML docs drop their "mirrors the legacy EventArgs /
    retained until Stage 5" notes.
  - **`NQueen.Kernel`** — `BitmaskSolver` removes both `event` declarations; `RaiseQueenPlaced` and
    `RaiseSolutionFound` become **sink-only** (`_onQueenPlaced?.TryWrite(...)` /
    `_onSolutionFound?.Report(...)`) under the same `EnableEvents && !_eventsSuppressedAfterCap`
    gate, and `Dispose` no longer nulls the events. `EnableEvents` is retained as the notification
    master-switch (it now gates the sinks rather than events).
  - **`NQueen.GUI`** — the vestigial `SubscribeToSimulationEvents` / `UnsubscribeFromSimulationEvents`
    methods (no-ops after Stage 4) and all seven call sites are deleted from `MainViewModel` and its
    `Commands` / `Validation` partials; the `_hasProgressTick` reset they performed is already done
    by `ResetProgress()` at simulation start.
- **Tests migrated off the events.** A shared synchronous `SynchronousProgress<T>` and a
  `CallbackChannelWriter<T>` are added to `NQueen.TestShared` so kernel/view-model tests observe the
  `OnSolutionFound` and `OnQueenPlaced` sinks deterministically on the producing thread. Six tests in
  `BitmaskSolverSingleModeTests`, `BitmaskSolverUniqueTests`, `BitmaskSolverAllModeTests`, and
  `SolverTests` are rewritten to pass the sinks via `SimulationContext` (the in-flight cancellation
  cases now flip `IsSolverCanceled` from the first sink callback); the `Fires*Events` names become
  `Pushes*Notifications`.
- **`IsSolverCanceled` is retained this stage** as the cancellation shim (it still backs the
  in-flight-cancellation tests and the Console / Benchmarks call sites); collapsing it onto the
  `CancellationToken` is a separate follow-up.

### Fixed (Event migration — Stage 4 follow-up: Visualize animation regressions)
- **Build-up animation halted after the first solution in Visualize mode** (most visible for
  N = 5 Unique, but it affected every Visualize run). Symptom: the queen-placement animation
  played until the first solution was found, then froze — subsequent solutions still streamed into
  the list, but the board never moved again. The `SelectedSolution` setter
  (`MainViewModel.Commands.cs`) unconditionally calls `StopVisualizationTimer()`, which since
  Stage 4 also **nulls `_queenPlacedReader`** and disposes the `DispatcherTimer`. Because
  `OnSolutionFoundReported` auto-selects every solution it finds (`SelectedSolution = sol`), the
  first solution tore down the animation pipeline; the keep-latest channel drain then short-circuited
  (`reader == null`) for the rest of the run. Pre-Stage-4 this was masked because the old
  `QueenPlaced` **event** re-rendered directly on the next placement; the channel drain has no such
  re-arm. **Fix:** the `SelectedSolution` setter now leaves the board to the animation timer while a
  Visualize run is live (`IsSimulating && DisplayMode == Visualize`) — it still tracks the selection
  but no longer stops the timer or statically repaints mid-run. The final solution is rendered at
  end-of-run by `ManageSimulationStatus(Finished)` exactly as before.
- **`SimulationCompleted` now fires *after* the final board render.** The completion event was
  raised before the end-of-run `ChessboardVm.PlaceQueens(first.Positions)` paint, so subscribers
  (and tests awaiting completion) could observe an unrendered board. Moving the
  `SimulationCompleted?.Invoke(...)` to the end of the `Finished` transition makes the fully-painted
  final state observable on completion — this latent ordering bug was surfaced (not caused) by the
  animation fix above. Full fast suite stays green: 424 unit + 89 view-model tests pass.
- **All-mode Visualize never animated (all solutions appeared at once).** Unlike Unique mode —
  which `HandleModeCommon` routes to `EnumerateUniqueVisualizeAdaptive()` when
  `DisplayMode == Visualize` — All mode always went through `EnumerateAllAdaptive(countOnly: false)`
  → `RunAllUnified()`, which **hardcodes** `DisplayMode.Hide`, `DelayInMillisec: 0`, and a **no-op
  `OnQueenPlaced`**, so it never streamed queen placements to the animation channel; only the
  `SolutionFound` sink fired, dumping every sample into the list instantly. **Fix:** added
  `EnumerateAllVisualizeAdaptive()` (in `BitmaskSolver.All.cs`), a two-phase path mirroring the
  Unique one — **Phase 1** runs the animated full-board DFS (streams `QueenPlaced` /
  `SolutionFound`, collects up to `MaxDisplayedCount` samples, then stops early at the
  cap); **Phase 2** computes the exact total via the fast half-board
  `BitboardNQueenSolver.CountSolutions`. `HandleModeCommon` now routes All+Visualize to it. Stopping
  Phase 1 at the cap is required because the engine sleeps `DelayInMillisec` between every placement
  while visualizing; visualization is capped at `MaxVisualizeBoardSize` (N ≤ 10) so the silent
  Phase-2 count is effectively instant. Full fast suite stays green: 424 unit + 89 view-model tests
  pass.
- **All-mode list selection rendered the wrong board for every non-first solution.** After an
  All-mode run finished, `SimulateAsync` rebuilds the list via `ExtractCorrectNoOfSols()`, which
  clears `ObservableSolutions` (the distinct boards streamed during the run) and repopulates it from
  `SimulationResults.Solutions`. Those come from `BuildResults`, which unpacks whatever key the
  solver stored in `_solutions`. All-mode stored `SymmetryHelper.GetCanonicalKey(rowsFound)` — the
  **canonical** (lexicographically-minimal) symmetry transform — at all three All-mode materialize
  sites (`RunAllUnified`, `EnumerateAllVisualizeAdaptive`, `CollectAllSampleSolutionsDFS`). Because
  All mode surfaces *every* variant, multiple distinct boards collapse onto the same canonical key,
  so clicking any list entry after the first re-rendered an identical placement. (Unique mode is
  unaffected: it pre-filters to `IsIdentityCanonical` boards, so each stored key already *is* the
  distinct board.) **Fix:** the three All-mode sites now store the **actual** board via
  `SymmetryHelper.PackRows(rowsFound)` (the exact inverse of `Solution.Unpack`), so each list entry
  renders its own placement. Full fast suite stays green: 424 unit + 89 view-model tests pass,
  including the All-mode distinctness regression guards.

### Changed (Event migration — Stage 6)
- **`IsSolverCanceled` collapsed onto the `CancellationToken`.** With every consumer now threading
  a real token through `SimulationContext.Cancellation` (Stages 1–5), the legacy `IsSolverCanceled`
  bool is removed as a redundant second source of truth for cancellation:
  - **`NQueen.Domain`** — `ISolverBackEnd` no longer exposes `IsSolverCanceled` (`UseCountOnlyAllMode`,
    `UseCountOnlyUniqueMode`, and `GetSimResultsAsync` are the only members that remain).
  - **`NQueen.Kernel`** — `BitmaskSolver` deletes the `IsSolverCanceled` field; the internal
    `IsCancellationRequested` property now reads `_cancellation.IsCancellationRequested` directly
    (no longer OR-ed with the bool). Every hot-loop check and every `BitmaskSearchEngine`
    `IsCanceled` callback in `.All.cs` / `.Single.cs` / `.Unique.cs` / `.CountUnique.cs` reads
    through that single property (the `(col & 0xF) == 0` throttle on the count-unique read is
    preserved).
  - **`NQueen.GUI`** — `SimulateAsync` continues to capture `CancellationTokenSource.Token` up front
    so a `Cancel()` that disposes/recreates the CTS cannot swap the source mid-run; the post-await
    guard, the catch-block guard, and the `finally`-block guard now read the captured local. The two
    `_solver.IsSolverCanceled = …` write sites in `Commands.cs` and the four `_solver.IsSolverCanceled`
    reads in `Events.cs` / `Progress.cs` are gone (the latter now read
    `CancellationTokenSource?.IsCancellationRequested == true`).
  - **Headless callers** — the dead `IsSolverCanceled = false` initialiser lines are removed from
    `NQueen.Console/Program.cs`, `NQueen.Console/Commands/DispatchCommands.cs` (three branches), and
    `NQueen.Benchmarking/ConsolePruningImpactBenchmarks.cs` (four benchmarks). With the flag gone
    they had no purpose — a default `CancellationToken` is never cancelled.
- **Tests migrated.** Five tests are rewritten to drive cancellation through a local
  `CancellationTokenSource` instead of toggling the deleted bool:
  - `BitmaskSolverSingleModeTests.SingleMode_VisualizePath_HonorsInFlightCancellation` —
    `cts.Cancel()` from the first `QueenPlaced` sink callback.
  - `BitmaskSolverUniqueTests.UniqueMode_VisualizePath_HonorsInFlightCancellation` — same pattern
    (N=8, Unique).
  - `BitmaskSolverAllModeTests.AllMode_Materialize_HonorsInFlightCancellation` — `cts.Cancel()` from
    the first `SolutionFound` sink callback.
  - `BitmaskSolverModeTests.GetSimResults_CancelledBeforeRun_ReturnsEmptyOrZero` — pre-cancelled
    token; the redundant `try/finally` reset is gone.
  - `SolverTests.BitmaskSolver_SingleMode_ShouldIgnorePreSetCancellationFlag` is **rewritten and
    renamed** to `BitmaskSolver_SingleMode_HonorsPreCancelledToken_ReturnsWithoutThrowing`,
    switching from synchronous `solver.Solve()` to `await solver.GetSimResultsAsync(ctx)` so the
    token actually reaches the kernel; the impossible-under-the-new-model
    `IsSolverCanceled.Should().BeFalse()` assertion is dropped.
- Verified by build (0 errors / 0 warnings) and the fast suite (**489 / 489 green** — 400 unit +
  89 view-model). The post-migration docs sweep (`README.md` Solver-Options preface,
  `.github/copilot-instructions.md` event-args note, `docs/ROADMAP.md` stale backlog bullet) ships
  alongside this entry — see *Docs* above.

### Changed (NQueen.Kernel)
- **Unified the two Visualize materialize paths into one method.**
  (`BitmaskSolver.All.cs`) and `EnumerateUniqueVisualizeAdaptive` (`BitmaskSolver.Unique.cs`) were
  byte-for-byte identical except for two mode-specific points, so they are now a single
  `EnumerateVisualizeAdaptive(bool isUnique)` in `BitmaskSolver.cs`. The two real differences are
  parameterised: Phase 1 applies the `IsIdentityCanonical` filter only when `isUnique` (All keeps
  every variant), and Phase 2 counts via `CountUniqueAdaptive` for Unique vs the symmetry-reduced
  `BitboardNQueenSolver.CountSolutions` for All. Sample storage is now uniformly
  `SymmetryHelper.PackRows` — correct for both modes because a Unique sample has already passed
  `IsIdentityCanonical`, so its raw packing equals its canonical key. This also dropped the
  unreachable `> 25` packed/raw branch from the old Unique path (Visualize is capped at
  `MaxVisualizeBoardSize` = 10). `HandleModeCommon` now routes both modes' Visualize runs to the
  shared method. Behaviour-preserving: full suite stays green (424 unit + 89 view-model).

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
