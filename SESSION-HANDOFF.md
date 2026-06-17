# Session Handoff: Step 3 — Unique CountOnly vs Materialize Gap Investigation

## Current State (2026-06-17)

**Branch**: `investigate/unique-materialize-gap`  
**Plan**: Step 3 investigation (from `docs/ROADMAP.md` line 596-598)  
**Step**: step-1 (in-progress) — Run `UniqueHighNBenchmark` to quantify the gap

## What Just Happened

1. **Plan registered** for Step 3: "Unique CountOnly vs Materialize gap" investigation
2. **Benchmark launched** in background:
   ```powershell
   cd D:\repos\Code\NQueen\NQueen.Benchmarking
   dotnet run -c Release --no-launch-profile -- --filter "*UniqueHighNBenchmark*" 2>&1 | Tee-Object baseline-unique-countonly-vs-materialize.log
   ```
3. **Benchmark was running** when the session ended (N=20 CountOnly was executing)
4. **Partial output captured** in `NQueen.Benchmarking/baseline-unique-countonly-vs-materialize.log`

## Partial Results Observed (from terminal output before interruption)

From the benchmark progress before cancellation:

| N  | Method              | Mean (observed)  | Notes |
|----|---------------------|------------------|-------|
| 16 | Unique_CountOnly    | 203.5 ms         | ✓ Complete |
| 16 | Unique_Materialize  | 201.3 ms         | ✓ Complete |
| 17 | Unique_CountOnly    | 1.477 s          | ✓ Complete |
| 17 | Unique_Materialize  | 1.478 s          | ✓ Complete |
| 18 | Unique_CountOnly    | 10.270 s         | ✓ Complete |
| 18 | Unique_Materialize  | 10.339 s         | ✓ Complete |
| 19 | Unique_CountOnly    | 1.391 m (83.5 s) | ✓ Complete |
| 19 | Unique_Materialize  | 1.371 m (82.3 s) | ✓ Complete |
| 20 | Unique_CountOnly    | (running)        | Interrupted |
| 20 | Unique_Materialize  | (not started)    | Pending |

### CRITICAL OBSERVATION

**The historical 5–6× gap does NOT exist anymore!**

Ratios from partial data:
- N=16: 201.3 / 203.5 = **0.99×** (Materialize is *faster* — noise)
- N=17: 1.478 / 1.477 = **1.00×** (essentially identical)
- N=18: 10.339 / 10.270 = **1.01×** (essentially identical)
- N=19: 82.3 / 83.5 = **0.99×** (essentially identical)

**The two-phase split in `EnumerateUniqueVisualizeAdaptive` (shipped earlier, documented in CHANGELOG.md lines 696-710) already closed the entire gap.**

## Next Actions When Resuming

### Option A: Complete the benchmark and document the closure
1. Check if `baseline-unique-countonly-vs-materialize.log` contains the full summary table (including N=20)
2. If incomplete, re-run the benchmark (takes ~15-20 min for ShortRunJob)
3. Parse the final table, compute final ratios
4. **Document the gap as CLOSED** in `docs/ROADMAP.md` and `CHANGELOG.md`
5. Move Step 3 from "Investigations" to shipped/closed status
6. Commit docs-only closure to this branch
7. Push and open PR

### Option B: If gap confirmed closed, move to next roadmap item
The backlog queue (after Step 3 closes) would typically move to:
- Next investigation candidate (check `docs/ROADMAP.md` Backlog section)
- Or pick up a different track (test coverage, refactoring, etc.)

## Files Modified/Created This Session

- `SESSION-HANDOFF.md` (this file)
- `NQueen.Benchmarking/baseline-unique-countonly-vs-materialize.log` (partial, untracked)
- Plan state is in memory/temp (VS managed)

## Commands to Resume

```powershell
# 1. Navigate to benchmarking directory
cd D:\repos\Code\NQueen\NQueen.Benchmarking

# 2. Check if the log file has the full summary table
Get-Content baseline-unique-countonly-vs-materialize.log | Select-String "Method|Mean|Ratio" -Context 2,15

# 3. If incomplete, re-run the benchmark
dotnet run -c Release --no-launch-profile -- --filter "*UniqueHighNBenchmark*" 2>&1 | Tee-Object baseline-unique-countonly-vs-materialize-FINAL.log

# 4. Once complete, parse the summary and update docs
```

## Key Context for Next Session

- **Historical gap claim**: ~5–6× Unique CountOnly vs Materialize at N=17–19
- **Actual current gap**: **~1.00× (NONE)** — the two-phase split already fixed it
- **Investigation outcome**: Step 3 should close as **"Gap already eliminated by shipped two-phase split (CHANGELOG.md line 696-710); no further action required"**
- **No production code changes needed** — this is a measure-and-document investigation
- **Test status**: 535/535 green (no changes made yet on this branch)

## References

- Plan: In-memory (VS-managed plan state)
- ROADMAP: `docs/ROADMAP.md` lines 596-598 (Step 3 investigation)
- CHANGELOG: `CHANGELOG.md` lines 696-710 (two-phase split that closed the gap)
- Benchmark class: `NQueen.Benchmarking/UniqueModeBenchmarks.cs` lines 100-132 (`UniqueHighNBenchmark`)
- Historical baseline: `NQueen.Benchmarking/BenchmarkDotNet.Artifacts/results/option-A-unique-mode-baseline.md`

---

**Resume with**: "Continue Step 3 investigation from session handoff" and the agent will pick up from this state.
