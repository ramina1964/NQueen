# Plan — Migrating Solver Events to Push-Sink Callbacks

> **Status:** Proposed (not started). Authored on branch `test/suite-review` for a *future*
> iteration. This document is the canonical spec for replacing the solver's `event`-based
> notification surface with per-call push sinks (`IProgress<T>` + `CancellationToken`, with a
> conflating `Channel<T>` for the high-frequency animation stream).
>
> **Convention.** When this work ships, fold the entry into `CHANGELOG.md` `[Unreleased]` and
> update `docs/ROADMAP.md` in the *same commit* (see "How to keep this document honest" in the
> roadmap). Delete or archive this file once the migration is complete.

---

## 1. Why (motivation)

The solver currently notifies the UI through three `event`s on `ISolverFrontEnd`:

| Event | Frequency | Purpose |
|---|---|---|
| `QueenPlaced` | **High** (per partial placement) | Drives the animated board |
| `SolutionFound` | Medium (per solution, up to cap) | Populates the solution list |
| `ProgressValueChanged` | Low (bucketed / heartbeat) | Drives the progress bar |

Plus two correlated control members: `SetSimulationToken(Guid)` (stale-run guard) and
`IsSolverCanceled` (cooperative cancellation polled inside handlers).

**Problems this causes today:**

1. **Lapsed-listener memory leak (believed mitigated — verify first).** `publisher.Event +=
   handler` makes the *publisher hold a strong reference to the subscriber*. The `MainViewModel`
   must call `SubscribeToSimulationEvents` / `UnsubscribeFromSimulationEvents` symmetrically; any
   missed `-=` (or a solver that outlives the VM) leaks the VM. This is the leak class previously
   hit. It has since been **manually mitigated** (symmetric unsubscribe, nulling the solver
   reference, and an `IDisposable` on the type that declares the handlers) — but that mitigation
   is **not yet verified** to remove the leak *correctly and completely*. **Pre-work for this
   migration:** investigate and confirm the current state (see §1a) so we know whether this
   migration is *corrective* (a live leak remains) or *preventive* (leak already gone; migration
   makes regression structurally impossible). Either way the migration removes the manual
   bookkeeping that can silently re-introduce it.
2. **Manual run-correlation plumbing.** The `Guid` simulation token
   (`SetSimulationToken` → `_currentSimToken` → `ProgressUpdateEventArgs.SimulationToken` →
   `OnProgressValueChangedEvent` guard) exists *only* to discard callbacks from a previous run.
3. **Hand-rolled UI marshalling.** Every handler wraps its body in `_uiDispatcher.Invoke(...)`.
4. **Test friction.** Tests/benchmarks must subscribe, unsubscribe, and call
   `SetSimulationToken`. The CHANGELOG already shows `SetSimulationToken` calls were stripped
   from benchmarks — this migration finishes that direction.

### 1a. Pre-work — verify the current leak mitigation is correct & complete

Before (or as the opening step of) the migration branch, audit whether the manual fix actually
holds. Concretely:

- **Every `+=` is a named-method handler with a matching `-=`.** Grep for lambda subscriptions
  (`+= (s, e) =>` / `+= delegate`) — a lambda cannot be removed by `-=` and leaks silently.
  Confirm `SubscribeToSimulationEvents` / `UnsubscribeFromSimulationEvents` are perfectly
  symmetric (same three handlers, same direction).
- **`Dispose()` is actually invoked.** WPF does not auto-dispose ViewModels. Trace who calls
  `MainViewModel.Dispose` (window `Closed`, DI scope disposal, host shutdown). An `IDisposable`
  that is never disposed never unsubscribes. If the VM is an app-lifetime DI singleton, the
  "leak" may simply never manifest — document that conclusion.
- **Reference direction.** Solver = publisher, VM = subscriber, `_solver` is a VM field; the leak
  only bites when a solver instance outlives the VM that subscribed, or when stale subscriptions
  accumulate across runs. The `UnsubscribeFromSimulationEvents()` call at the top of
  `SubscribeToSimulationEvents()` already guards accumulation — confirm it covers every path.
- **Evidence, not assertion.** Confirm with a measurement: a dotMemory / VS Diagnostic Tools
  snapshot across N simulate-runs showing `MainViewModel` instance count stays flat (no retained
  instances rooted by a solver delegate), or a focused unit test using a `WeakReference` to the
  VM that asserts collection after a run + `GC.Collect()`.

Record the finding in `CHANGELOG.md` / `docs/ROADMAP.md`; it determines the framing (corrective
vs preventive) of this whole track.

**Key enabling fact:** the kernel is *already* callback-based under the hood. The events are
thin adapters over existing internal sinks, so this is a **delete-the-adapter** refactor, not a
producer rewrite.

---

## 2. Current surface (as-built inventory)

### Interfaces — `NQueen.Domain/Interfaces`
- `ISolverFrontEnd` — `DelayInMillisec`, `ProgressValue`, the three `event`s, `SetSimulationToken(Guid)`.
- `ISolverBackEnd` — `IsSolverCanceled`, `UseCountOnlyUniqueMode`, `UseCountOnlyAllMode`, `GetSimResultsAsync(SimulationContext)`.
- `ISolver : ISolverBackEnd, ISolverFrontEnd`.

### Event-arg types — `NQueen.Domain/EventArgs`
- `QueenPlacedEventArgs` — `readonly struct`; `Memory<int> Solution`, `int BoardSize`, `UInt128 PackedCanonical`.
- `SolutionFoundEventArgs` — `sealed class : EventArgs`; same shape.
- `ProgressUpdateEventArgs` — `class : EventArgs`; `double Value`, `Guid SimulationToken`.

### Producer — `NQueen.Kernel/Solvers`
- `BitmaskSolver.cs` — declares the three events; `EnableEvents` gate (default `true`);
  `SetSimulationToken` → `_currentSimToken`; `GetSimResultsAsync` → `Solve()`.
- `BitmaskSolver.Single.cs` — raise sites at lines ~55, 68, 81–87, 96–97, 126, 161–162.
- `BitmaskSolver.Unique.cs` — raise sites at ~63–67, 92, 117–120, 141–142, 157, 200–201.
- `BitmaskSolver.All.cs` — engine request with `OnQueenPlaced` / `OnSolution` at ~31–32.
- **Already-callback internals (no change to their shape):**
  `BitmaskSearchEngine` (`Func<int[],bool> OnSolution`, `Action<Memory<int>> OnQueenPlaced`),
  `BitmaskParallelEngine.Unique` (`Action<int[]>`, `Action<ulong>`, `Action<double>`),
  `ProgressReporter` (`Action<double>` with bucket + heartbeat throttling),
  `SymmetryPrunedUniqueCounter.Count(..., Action<int[]>? onMaterialized)`.
- Cap suppression: `_eventsSuppressedAfterCap` (sample-cap logic — **orthogonal to events**, keep it).

> ⚠️ **Known inconsistency to resolve during Stage 0 (not after).** The `EnableEvents` gate is
> applied *unevenly* to progress raises:
> - **Gated** — intermediate progress in `.Unique.cs` (~line 119):
>   `p => { if (EnableEvents) ProgressValueChanged?.Invoke(...); }`.
> - **Ungated** — the terminal `100.0` raises in `.Single.cs` (~lines 55, 68) and `.Unique.cs`
>   (~lines 92, 157) call `ProgressValueChanged?.Invoke(... 100.0 ...)` with **no `EnableEvents`
>   check**. So even with `EnableEvents = false`, a consumer still receives one final 100%
>   callback on Single/Unique runs.
>
> This is harmless today (the only headless consumers ignore it) but it is exactly the kind of
> latent quirk that must **not** be carried silently into the new design. The Stage 0 seam
> (`RaiseProgress`) centralises the gate so the "report 100% even when sinks are off?" policy is
> decided **once**, in one place. Recommended target policy: a null `OnProgress` sink reports
> *nothing* (terminal 100% included) — uniform with the other two streams.

### Consumer — `NQueen.GUI/ViewModels`
- `MainViewModel.Events.cs` — `SubscribeToSimulationEvents` / `UnsubscribeFromSimulationEvents`,
  `OnQueenPlacedEvent` (throttled via `DispatcherTimer`; synchronous `_uiDispatcher.Invoke` on the
  no-delay path), `OnSolutionFoundEvent` (`_batchedSolutions` / `ObservableSolutions`),
  `OnProgressValueChangedEvent` (token-guarded).
- `MainViewModel.Commands.cs` — `SimulateAsync` sets `IsSolverCanceled = false`,
  `_currentSimulationToken = Guid.NewGuid()`, `SetSimulationToken(token)`, builds
  `SimulationContext`, `await GetSimResultsAsync`.

### Tests / benchmarks touching the surface
- `NQueen.ViewModelTests/Tests/Solver/SolverTests.cs`
- `NQueen.UnitTests/Tests/Kernel/BitmaskSolverAllModeTests.cs`
- `NQueen.ViewModelTests` `ProgressRelayTests` (heartbeat synthetic-progress test)
- `NQueen.Benchmarking/ConsolePruningImpactBenchmarks.cs` (sets `EnableEvents = false`)

---

## 3. Target design — per-call push sinks

**One rule, applied consequently:** the solver never *exposes* long-lived events; each call
*receives* the sinks it should push to. Lifetime = the call. Nothing to unsubscribe.

### 3.1 New notification payloads — `NQueen.Domain` (plain records, not `EventArgs`)
```
public readonly record struct QueenPlacedInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical);
public readonly record struct SolutionFoundInfo(Memory<int> Solution, int BoardSize, UInt128 PackedCanonical);
public readonly record struct ProgressInfo(double Percent);   // NOTE: no Guid token — see §3.3
```
(Reuse the existing `EventArgs` structs if you prefer minimal churn; the only material change is
*dropping the `Guid` token* from the progress payload.)

### 3.2 Sink bundle carried by `SimulationContext`
`SimulationContext` is already the single parameter object for `GetSimResultsAsync`. Extend it
with **optional** sinks (null = "don't report", replacing the `EnableEvents=false` idiom):
```
public record SimulationContext(
	int BoardSize,
	SolutionMode SolutionMode,
	DisplayMode DisplayMode,
	IProgress<ProgressInfo>?     OnProgress       = null,
	IProgress<SolutionFoundInfo>? OnSolutionFound = null,
	ChannelWriter<QueenPlacedInfo>? OnQueenPlaced = null,   // conflating — see §3.4
	CancellationToken            Cancellation     = default);
```

### 3.3 Run correlation — **deleted**
A fresh `Progress<T>` is created per `SimulateAsync` call, so a previous run's sink simply no
longer exists. `SetSimulationToken`, `_currentSimToken`, and `ProgressUpdateEventArgs.SimulationToken`
are all **removed**. The `OnProgressValueChangedEvent` token guard disappears.

### 3.4 The high-frequency stream — conflating `Channel<T>`
`QueenPlaced` can fire very fast at large N with zero delay. `IProgress<T>.Report` does a
`Post` per call and could flood the dispatcher. Model *only this stream* as a bounded,
**drop-oldest / keep-latest** channel consumed by the existing `DispatcherTimer`:
```
Channel.CreateBounded<QueenPlacedInfo>(
	new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
```
This matches the UI's real need ("show the most recent prefix") and guarantees the producer is
never blocked by a slow UI. Progress and Solutions stay on `IProgress<T>`.

### 3.5 Cancellation
Replace the `IsSolverCanceled` bool with the `CancellationToken` in `SimulationContext`, wired to
the `IAsyncRelayCommand`'s built-in cancel support (CommunityToolkit.Mvvm is already referenced).
Internal hot-loop checks become `token.IsCancellationRequested` (keep the existing
`(col & 0xF) == 0` throttle on the read).

---

## 4. Staged execution (each stage independently shippable & green)

> Order chosen so the build stays green between stages and risk rises last. Run the **fast** test
> suite (`Fast.runsettings`) after each stage; full suite before merge.

**Stage 0 — Extract the notification seam (behaviour-preserving; do this FIRST).**
- *Why:* the raise logic is currently inlined across ~16 call sites in `.Single.cs` /
  `.Unique.cs` / `.All.cs`. Collapsing them behind three private helpers turns every later stage
  into a **1-helper-body change** instead of a multi-file raise-site hunt — smaller diffs, easier
  review, trivial to bisect if the animation regresses.
- Add three private methods to `BitmaskSolver` and route **every** raise site through them:
  ```
  private void RaiseProgress(double percent);                       // owns the EnableEvents gate
  private void RaiseQueenPlaced(Memory<int> rows, int boardSize);   // owns the gate + cap suppression
  private void RaiseSolutionFound(int[] rows, int boardSize);       // owns the gate + cap suppression
  ```
- **Resolve the `EnableEvents` inconsistency here** (see ⚠️ in §2): the terminal `100.0` progress
  raises in `.Single.cs` (~55, 68) and `.Unique.cs` (~92, 157) are currently **ungated**; the
  intermediate ones are gated. Centralise the gate in `RaiseProgress` and apply it **uniformly**
  (a disabled/absent progress sink reports nothing, terminal 100% included).
- This is pure extract-method: **zero behaviour change** beyond the deliberate gate-uniformity fix,
  fully covered by existing `BitmaskSolver*Tests`. Ships green as the first commit of the
  migration branch *before* any sink is introduced.

**Stage 1 — Progress → `IProgress<ProgressInfo>`; delete the Guid token.**
- Add `ProgressInfo`; add `OnProgress` to `SimulationContext`.
- In `BitmaskSolver`, forward the internal `Action<double>` to `simContext.OnProgress?.Report(...)`.
- VM: build `new Progress<ProgressInfo>(p => SetProgressPercent(...))`; delete the token guard.
- Remove `SetSimulationToken` from `ISolverFrontEnd`, `BitmaskSolver`, VM, tests, benchmarks.
- *Highest value / lowest risk; deletes the most plumbing.*

**Stage 2 — Cancellation → `CancellationToken`.**
- Add `Cancellation` to `SimulationContext`; thread into `Solve()` and hot loops.
- VM: use the `IAsyncRelayCommand` cancel token; remove `IsSolverCanceled` writes.
- Keep `IsSolverCanceled` as a thin shim for one stage if needed, then delete in Stage 5.

**Stage 3 — `SolutionFound` → `IProgress<SolutionFoundInfo>`.**
- Replace the event raise sites in `.Single` / `.Unique` / `.All` with `OnSolutionFound?.Report(...)`.
- VM `OnSolutionFoundEvent` body becomes the `Progress<T>` callback (drop `_uiDispatcher.Invoke`).

**Stage 4 — `QueenPlaced` → conflating `Channel<T>`.**
- Producer writes to `ChannelWriter<QueenPlacedInfo>` (`TryWrite`, non-blocking).
- VM consumes via the **existing** `DispatcherTimer` tick (drain latest, render prefix).
- Validate animation parity carefully (see Risks §6).

**Stage 5 — Remove the event scaffolding.**
- Delete the three `event`s from `ISolverFrontEnd` and `BitmaskSolver`.
- Delete `SubscribeToSimulationEvents` / `UnsubscribeFromSimulationEvents`, `_currentSimToken`,
  `IsSolverCanceled` (now token-based). Re-evaluate `EnableEvents` → replaced by "all sinks null".
- Decide fate of `EventArgs` types (delete or repurpose as the `Info` records).

**Stage 6 — Tests, benchmarks, docs.**
- Update `SolverTests`, `BitmaskSolverAllModeTests`, `ProgressRelayTests`, benchmarks.
- `CHANGELOG.md` `[Unreleased]`, `docs/ROADMAP.md` (Current State + remove any tracked item),
  `README.md` Solver-Options table, `.github/copilot-instructions.md` (the `Memory<int>`
  event-args convention note will need rewording).

---

## 5. Files to touch (checklist)

| Area | File(s) |
|---|---|
| Payloads | `NQueen.Domain/EventArgs/*` (repurpose or replace with `*Info` records) |
| Context | `NQueen.Domain/Context/SimulationContext.cs` |
| Interfaces | `ISolverFrontEnd.cs`, `ISolverBackEnd.cs`, `ISolver.cs` |
| Producer | `BitmaskSolver.cs`, `.Single.cs`, `.Unique.cs`, `.All.cs` (raise sites only) |
| Consumer | `MainViewModel.Events.cs`, `MainViewModel.Commands.cs` |
| Tests | `SolverTests.cs`, `BitmaskSolverAllModeTests.cs`, `ProgressRelayTests` |
| Benchmarks | `ConsolePruningImpactBenchmarks.cs` (and any using `EnableEvents`) |
| Docs | `CHANGELOG.md`, `docs/ROADMAP.md`, `README.md`, `.github/copilot-instructions.md` |

*Internal engines (`BitmaskSearchEngine`, `BitmaskParallelEngine.*`, `ProgressReporter`,
`SymmetryPrunedUniqueCounter`) need **no signature change** — they are already callback-based.*

---

## 6. Risks & mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| **Animation timing change** — `Progress<T>.Report`/channel are async `Post`s; the current no-delay path uses *synchronous* `_uiDispatcher.Invoke`. Ordering/feel of the board could shift. | **High** | Keep `QueenPlaced` on the existing `DispatcherTimer` drain (Stage 4); A/B the animation by eye at N≈8–12 with delay 0 and >0. |
| Channel back-pressure / dropped frames look wrong | Med | `DropOldest` with capacity 1 is intentional ("latest prefix"); the timer renders at a human-visible rate regardless. |
| `ProgressRelayTests` heartbeat semantics rely on token/`IsSimulating` | Med | Re-point the test at the `Progress<T>` sink; assert reported `ProgressInfo` values instead of token routing. |
| Hidden subscribers beyond the VM | Low | `Find All References` on each event before deleting (Console runner sets `EnableEvents=false`, so unlikely). |
| Scope creep into the kernel hot path | Low | Engines are untouched; only `BitmaskSolver` adapter lines change. Re-run `UniqueFastHalfBoardEvenOddBenchmark` to confirm no regression. |

---

## 7. Is it worth it? (conciseness / readability / maintainability / performance / labor)

| Dimension | Verdict | Rationale |
|---|---|---|
| **Conciseness** | ✅ **Net deletion** | Removes `SetSimulationToken`, `_currentSimToken`, the `Guid` token field on the progress payload + its guard, `Subscribe`/`Unsubscribe` methods, and (eventually) `IsSolverCanceled`. More code deleted than added. |
| **Readability** | ✅ Improved | Sinks are passed explicitly per run; intent is local and obvious. `Progress<T>` captures the UI `SynchronizationContext`, removing most `_uiDispatcher.Invoke` boilerplate. |
| **Maintainability** | ✅ **Strong win** | The lapsed-listener leak becomes *structurally impossible* — no `+=` outlives a run. No lifetime bookkeeping. Far easier to test (pass a capturing `IProgress<T>` fake). |
| **Performance** | ⚖️ Neutral | Headless/benchmark: null sink = zero overhead (same as `EnableEvents=false`). GUI: `Progress<T>` merely *relocates* the dispatcher marshalling the VM already does. Hot stream stays throttled via the existing timer + conflating channel, so no flood. Kernel hot path is untouched. |
| **Labor / risk** | ⚖️ **Moderate, but lower than usual** | Touches Domain + Kernel + GUI + tests, **but** the kernel is already callback-based, so it's an adapter-deletion. The single genuine risk is animation-timing parity (mitigated in §6). Six independently-shippable stages keep risk bounded. |

**Bottom line:** **Worth doing** — but its leak benefit is **preventive, pending the §1a
verification**: the lapsed-listener leak was manually mitigated (symmetric unsubscribe + nulling +
`IDisposable`), so confirm whether any live leak remains before claiming this as *corrective*.
Regardless of that outcome, the migration makes the leak *structurally impossible* (no `+=`
outlives a run) and deletes the manual bookkeeping that can silently re-introduce it. Do it
*staged*: begin with **Stage 0** (extract the notification seam + fix the `EnableEvents`
gate-uniformity quirk; behaviour-preserving), then **Stage 1** (progress + token deletion) for the
best value-to-risk ratio, and **guard the animation parity** in Stage 4. It is not a "rewrite"; it
is removing a shim the kernel never needed.

---

## 8. Acceptance criteria

- [ ] Stage 0 seam in place: all raises route through `RaiseProgress` / `RaiseQueenPlaced` / `RaiseSolutionFound`.
- [ ] §1a leak audit done: current manual mitigation verified (or a residual leak identified) with evidence (WeakReference test / memory snapshot); finding recorded.
- [ ] `EnableEvents` gate applied **uniformly** — no ungated terminal 100% progress raise survives.
- [ ] No `event` declarations remain on `ISolverFrontEnd` / `BitmaskSolver`.
- [ ] No `Guid` simulation token anywhere (`SetSimulationToken`, `_currentSimToken`, payload field gone).
- [ ] Cancellation flows through `CancellationToken`; `IAsyncRelayCommand` cancel works.
- [ ] `MainViewModel` has no `+=` / `-=` on solver notifications.
- [ ] Board animation is visually equivalent at delay = 0 and delay > 0 (manual A/B).
- [ ] Full test suite green; `UniqueFastHalfBoardEvenOddBenchmark` within ±noise of baseline.
- [ ] `CHANGELOG.md`, `docs/ROADMAP.md`, `README.md`, `.github/copilot-instructions.md` updated.
