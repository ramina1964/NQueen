# Refactor TODO — refactor/consolidate branch

## Context
Partial refactor of `NQueen.Kernel` was started but hit the tool call limit mid-session.
Steps 1 and 2 were completed; steps 3–8 were not. The build was **never run** — compilation
state is unknown. Resume from Step 3.

---

## How to start the next session

Paste this prompt at the very beginning:

> "I'm continuing an incomplete refactor on the `refactor/consolidate` branch of NQueen.
> Please read `REFACTOR_TODO.md`, check the current state of the listed files, run the build
> first to see if it compiles, then continue from the first unchecked step."

---

## Completed steps ✅

- [x] **Step 1** — Added `PrefixMinimalityPruning` and `ReflectionPruning` bool fields (with
  `= false` defaults) to `BitmaskSearchEngine.Request`; updated `MainLoop` and
  `MainLoopCountOnly` to read from `request` instead of `SearchOptimizations` statics.

- [x] **Step 2** — Updated all `BitmaskSearchEngine.Run(new Request(...))` call-sites in
  `BitmaskSolver.cs`, `BitmaskSolver.All.cs`, and `BitmaskSolver.Unique.cs` to pass
  `PrefixMinimalityPruning` and `ReflectionPruning` via the request; removed the preceding
  `SearchOptimizations.Configure()` calls that only served to set those global flags.

---

## Remaining steps ❌

- [ ] **Step 3** — Remove global mutable state from `SearchOptimizations.cs`:
  - Delete the three `static volatile` fields:
	`PrefixMinimalityPruningEnabled`, `ReflectionPrefixPruningEnabled`, `IncrementalCanonicalizationEnabled`
  - Delete both `Configure()` overloads
  - Keep only the pure static helper method `ShouldPrunePrefixIncremental`
  - Also remove the remaining `Engines.SearchOptimizations.Configure(...)` call in
	`BitmaskSolver.Unique.cs` (`ExecuteUniqueModeUnified`) — it only set
	`IncrementalCanonicalizationEnabled` which is now always `false`

- [ ] **Step 4** — Remove the duplicate `ProgressReporter` struct from the **bottom** of
  `BitmaskSearchEngine.cs` (namespace `NQueen.Kernel.Solvers`). The canonical one lives in
  `NQueen.Kernel\Solvers\Engines\ProgressReporter.cs` (namespace `NQueen.Kernel.Solvers.Engines`).
  Update any usages inside `BitmaskSearchEngine.cs` to use `Engines.ProgressReporter`.

- [ ] **Step 5** — Remove the dead `ReportRootProgress` method from `BitmaskSearchEngine.cs`
  (it is defined but never called).

- [ ] **Step 6** — Remove `GenerateConstructiveSingleSolution` from `BitmaskSolver.Single.cs`
  (it is identical to `GenerateConstructiveSolution` in `BitmaskSolver.cs`). Replace the one
  call-site with a call to `GenerateConstructiveSolution`.

- [ ] **Step 7** — Inline `EnumerateAllAndReturnCount` (trivial one-liner wrapper in
  `BitmaskSolver.cs`). Remove the method and inline its body at the single call-site in
  `HandleModeCommon`.

- [ ] **Step 8** — Run the build and fix any compilation errors. Run existing unit tests to
  verify no regressions.

---

## Key files involved

| File | What changes |
|---|---|
| `NQueen.Kernel\Solvers\Engines\SearchOptimizations.cs` | Step 3: remove fields + Configure overloads |
| `NQueen.Kernel\Solvers\BitmaskSolver.Unique.cs` | Step 3: remove last Configure() call |
| `NQueen.Kernel\Solvers\BitmaskSearchEngine.cs` | Steps 4 + 5: remove duplicate ProgressReporter + dead method |
| `NQueen.Kernel\Solvers\BitmaskSolver.Single.cs` | Step 6: remove duplicate constructive generator |
| `NQueen.Kernel\Solvers\BitmaskSolver.cs` | Step 7: inline EnumerateAllAndReturnCount |

---

## Cleanup
Delete this file and commit once all steps are done and the build is green.
