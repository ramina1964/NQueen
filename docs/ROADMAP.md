# NQueen — Roadmap

A single canonical document for *where the project is* and *what's next*.
Update this file whenever a tracked item completes or a new one is added,
in the same change that touches `CHANGELOG.md`.

> **Convention.** `CHANGELOG.md` records what shipped. This file records what is
> in flight, planned, or being weighed up. When an item ships it is moved out of
> here into the appropriate `[Unreleased]` block in `CHANGELOG.md`.

---

## Current State

| Item | Value |
|---|---|
| Latest release | **1.0.0** — 2026-05-29 (merged from `refactor/consolidate`) |
| Active branch | `test/kernel-coverage` |
| Target framework | .NET 10 across all projects (`net10.0` / `net10.0-windows` for GUI) |
| Test count | **498 / 498 passing** (409 unit + 89 view-model; up from 304 at v1.0.0) |
| Code coverage | Stale (last full run 2026-05-29: Domain 93 %, Kernel 67 %, Shared 95 %, Total 77 %). Re-collect pending. |
| Build status | 0 errors / 0 warnings |

### Recently shipped (see `CHANGELOG.md` `[Unreleased]` for full detail)

- Kernel performance: `TZCNT` intrinsic, `SearchState` struct, `EnsureMinThreads` one-shot guard.
- Kernel correctness: two-phase `EnumerateUniqueVisualizeAdaptive` (~2× fewer nodes for Unique Visualize).
- Kernel correctness: fixed a Unique-mode count under-report at N >= 16
  (`CountUniqueFastHalfBoard` returned 692 857 instead of 1 846 955 at N = 16); the
  shared forward-prefix prune `ShouldPrunePrefixFull` is now reflection-only after
  removing an unsound rotate-180 minimality test.
- Kernel refactor: `BitmaskSolver.cs` further split into `BitmaskSolver.CountUnique.cs` and `BitmaskSolver.Materialize.cs` (710 → 268 lines in the root file).
- Test coverage: dedicated tests for `BitmaskSolver.CountUnique.cs` (14 tests),
  `BitmaskSolver.Single.cs` (17 tests), `BitmaskSolver.All.cs` (17 tests —
  12 declarations + 5 Theory expansions), and `BitmaskSolver.Unique.cs`
  (15 tests — 11 declarations + 4 Theory expansions).
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
| `BitmaskSolver.Materialize.cs` | _planned_ | **next** | TBD — pending re-collect |

After every entry above is shipped, regenerate the coverage report and update the
baseline column with the new numbers.

---

## Backlog — Kernel Correctness

- **`GenerateConstructiveSolution` `n % 6 == 3` branch emits an invalid placement.**
  Surfaced via `BitmaskSolverSingleModeTests.SingleMode_ConstructivePath_N15_RoutesAndReturnsCount1`,
  which currently asserts only the routing and count (not placement validity) because
  the constructed rows for N = 15 contain a diagonal conflict. Source: defined in
  `NQueen.Kernel/Solvers/BitmaskSolver.Materialize.cs` (the `else` branch around
  `for (int i = 2; i <= n - 1; i += 2) ...`). Fix: correct the construction for
  `n % 6 == 3` (likely a known textbook variant) and tighten the test to call
  `AssertValidPlacement` for N = 15.

---

## Backlog — Kernel Performance

Consolidated from `docs/ignored/Archive/Code Analysis - 02-02.2026.txt` and
`docs/ignored/Archive/Potential All Mode Improvements.txt`. Items are listed roughly by
effort × expected impact.

### Small wins, low risk

- **Throttle `IsSolverCanceled` reads** in `CountUniqueFastHalfBoard.__DFS` hot loop
  (don't check on every `while` iteration). Source: `Code Analysis - 02-02.2026.txt`.
- **Tighten `ShouldPrunePrefixFull` gating** so it's only called when `col >= pruneGate`
  and reflection pruning is enabled. Source: same.

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
- **CPU utilisation drop at N = 19 Unique CountOnly** — recent measurement shows
  usage falls under 10 % after ~35 % progress. Root cause likely tail imbalance
  in the parallel root partitioning. Depends on the work-stealing item above.

---

## Backlog — GUI

From `docs/ignored/Archive/Code Analysis - 02-02.2026.txt`. None of these are addressed in
`[Unreleased]` so far.

1. **Chessboard not refreshed on board-size change without tab-out** — when the user
   types a new size then clicks Simulation without leaving the field, the board
   does not redraw.
2. **Input panel and chessboard not vertically aligned** — outer grid columns 1 and
   2 are top-misaligned at default window size.
3. **Gap between outer grid (1,1) and (1,2) grows on resize** — should stay constant
   from maximised window down through smaller sizes.
4. **Excess right-margin white space** — window width should be set to the sum of
   the three column widths plus a conventional margin.

---

## How to keep this document honest

When a tracked item ships, in the **same commit**:

1. Move the item out of this file (delete the row / bullet, or flip "in progress" → remove).
2. Add a corresponding entry to `CHANGELOG.md` under `[Unreleased]`.
3. If the change shifts the "Current State" table (test count, coverage, branch name,
   build status), update those numbers here.
