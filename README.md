# NQueen

A combined **Console** and **WPF desktop** application for solving the N-Queens problem,
implemented in C# 14 / .NET 10. The solver uses a symmetry-pruning backtracking algorithm
with parallel execution and bitmask state representation, and can enumerate solutions
up to N = 25 (or count-only up to N = 64).

[![CI](https://github.com/ramina1964/NQueen/actions/workflows/ci.yml/badge.svg)](https://github.com/ramina1964/NQueen/actions/workflows/ci.yml)

---

## Table of Contents

- [Features](#features)
- [Algorithm](#algorithm)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Build](#build)
- [Run — Console](#run--console)
- [Run — WPF GUI](#run--wpf-gui)
- [Solver Options](#solver-options)
- [Known Solution Counts](#known-solution-counts)
- [Benchmark Results](#benchmark-results)
- [Contributing](#contributing)
- [License](#license)

---

## Features

| Feature | Detail |
|---|---|
| **Three solution modes** | **All** — every distinct placement; **Unique** — canonical (up to rotation/reflection); **Single** — one solution |
| **Two output modes** | **Materialize** — up to 5 sample solutions displayed; **Count-only** — exact count, no storage |
| **Bitmask DFS** | 64-bit column / diagonal masks; `TZCNT` intrinsic for candidate iteration |
| **Symmetry pruning** | Prefix-minimality pruning + partial-reflection pruning halve the effective search space |
| **Half-board restriction** | Vertical-symmetry shortcut for All mode (N ≥ 15) doubles throughput |
| **Parallel execution** | `Parallel.ForEach` with adaptive root-split depth; scales across all available cores |
| **Precomputed lookup** | Exact counts for N = 1–29 (OEIS A000170 / A002562); N ≥ 21 returns instantly |
| **WPF GUI** | Animated step-by-step visualisation, save results to file, MVVM + CommunityToolkit |
| **Interactive CLI** | Menu-driven mode + board-size selection, or fully non-interactive via flags |

---

## Algorithm

The core algorithm is a **bitmask backtracking DFS**:

1. Represent occupied columns, forward-diagonals, and back-diagonals as three `ulong` masks.
2. At each column, compute `available = ~(cols | d1 | d2) & fullMask` to obtain candidate rows in a single instruction.
3. Iterate candidates with `bit = avail & -avail` (lowest set bit) and advance via `TZCNT`.
4. **Symmetry pruning** — at each depth ≥ `pruneDepthGate`, test the partial prefix against its reflection/rotation canonical form; prune entire sub-trees that cannot yield a canonical solution.
5. **Half-board** — for All mode, restrict the first-column queen to rows `0 … ⌊N/2⌋` and double the count, exploiting vertical symmetry.
6. **Parallelism** — the root row loop is partitioned across cores via `Partitioner.Create`; each thread runs an independent DFS with thread-local state.
7. **Count-only path** — for Unique mode at N ≥ 16, a dedicated half-board parallel DFS (`CountUniqueFastHalfBoard`) avoids any solution materialisation.

---

## Project Structure

```
NQueen/
├── NQueen.Domain/          Interfaces, models, enums, event-args, settings, utilities
├── NQueen.Kernel/          BitmaskSolver (partial), engines, symmetry helpers
│   └── Solvers/            BitmaskSolver.cs · .All · .Single · .Unique
├── NQueen.Shared/          Cross-cutting helpers (parsing, numerics)
├── NQueen.GUI/             WPF MVVM front-end (net10.0-windows)
├── NQueen.Console/         Console runner (net10.0)
├── NQueen.UnitTests/       Kernel / domain unit tests
├── NQueen.ViewModelTests/  ViewModel integration tests
├── NQueen.TestShared/      Shared test infrastructure
└── NQueen.Benchmarking/    BenchmarkDotNet benchmarks
```

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | **10.0** or later |
| OS (GUI) | Windows 10 / 11 (WPF requires `net10.0-windows`) |
| OS (Console) | Windows, Linux, or macOS (`net10.0`) |

---

## Build

```bash
# Clone
git clone https://github.com/ramina1964/NQueen.git
cd NQueen

# Build all projects
dotnet build --configuration Release
```

Run all tests:

```bash
dotnet test --configuration Release --no-build
```

---

## Run — Console

### Interactive menu

```bash
dotnet run --project NQueen.Console -c Release
```

The menu prompts for solution mode, board size, and whether to use count-only output.

### Non-interactive (flags)

```bash
dotnet run --project NQueen.Console -c Release -- [options]
```

| Flag | Default | Description |
|---|---|---|
| `--mode <all\|unique\|single>` | `all` | Solution mode |
| `--size <N>` | `8` | Board size (1 – 64) |
| `--count-only` | off | Count only; suppress sample output |
| `--materialize` | on | Show up to 5 sample solutions |
| `--halfboard` | off | Half-board restriction (All mode, N ≥ 15) |
| `--help` | — | Print usage |

**Examples**

```bash
# Count all solutions for N=15 using half-board shortcut
dotnet run --project NQueen.Console -c Release -- --mode all --size 15 --count-only --halfboard

# Show 5 sample unique solutions for N=12
dotnet run --project NQueen.Console -c Release -- --mode unique --size 12

# Single solution for N=20
dotnet run --project NQueen.Console -c Release -- --mode single --size 20
```

---

## Run — WPF GUI

Open `NQueen.sln` in **Visual Studio 2022 / 2026** (Windows), set `NQueen.GUI` as startup project, and press **F5**.

The GUI provides:
- Board-size slider and solution-mode selector
- **Hide** (count + samples), **Visualize** (animated step-by-step), and **Single** display modes
- Progress bar and elapsed-time display
- **Save results** button (exports solution list to a text file)

---

## Solver Options

The following properties on `BitmaskSolver` control the solver behaviour (set before calling `Solve()`):

| Property | Default | Description |
|---|---|---|
| `EnablePrefixMinimalityPruning` | `false` | Prune prefixes whose canonical form is lexicographically greater than the current partial solution |
| `EnablePartialReflectionPruning` | `false` | Prune partial solutions whose horizontal reflection is already (or will be) enumerated |
| `EnableHalfBoardRestriction` | `false` | Restrict first-column queen to top half; doubles count (All mode, N ≥ 15) |
| `UseAdaptiveDepth` | `false` | Auto-choose `ParallelRootSplitDepth` based on board size and core count |
| `ParallelRootSplitDepth` | `1` | Number of prefix columns fixed before work is partitioned across threads |
| `UseParallel` | `true` | Enable parallel execution |
| `UseCountOnlyAllMode` | `false` | Count-only path for All mode (no materialisation) |
| `UseCountOnlyUniqueMode` | `false` | Count-only path for Unique mode |

The Console runner and GUI both set `EnablePrefixMinimalityPruning = true`,
`EnablePartialReflectionPruning = true`, and `UseAdaptiveDepth = size >= 14` automatically.

---

## Known Solution Counts

Sourced from OEIS [A000170](https://oeis.org/A000170) (All) and [A002562](https://oeis.org/A002562) (Unique).

| N | All solutions | Unique solutions |
|--:|--:|--:|
| 1 | 1 | 1 |
| 4 | 2 | 1 |
| 5 | 10 | 2 |
| 6 | 4 | 1 |
| 7 | 40 | 6 |
| 8 | 92 | 12 |
| 9 | 352 | 46 |
| 10 | 724 | 92 |
| 11 | 2 568 | 341 |
| 12 | 14 200 | 1 787 |
| 13 | 73 712 | 9 233 |
| 14 | 365 596 | 45 752 |
| 15 | 2 279 184 | 285 053 |
| 16 | 14 772 512 | 1 846 955 |
| 17 | 95 815 104 | 11 977 939 |

Counts for N = 1–29 are precomputed in `ExpectedSolutionCounts`; N ≥ 21 are returned from the lookup table without enumeration.

---

## Benchmark Results

Measured on **Intel Core i7-14700K** (28 logical / 20 physical cores), .NET 10.0.8, x64 RyuJIT AVX2.
`ShortRunJob`, 2 warmup iterations, 5 measurement iterations.

### All mode — "new" Console config vs "old" baseline

| N | Old (no pruning) | New (pruning on) | Δ |
|--:|--:|--:|--:|
| 12 | 360 µs | 355 µs | −1% |
| 14 | 4.72 ms | 4.66 ms | −1% |
| 16 | 188 ms | 197 ms | flat (noise) |

### Unique mode — "new" Console config vs "old" baseline

| N | Old (no pruning) | New (pruning on) | Δ |
|--:|--:|--:|--:|
| 12 | 955 µs | 836 µs | **−12%** |
| 14 | 13.8 ms | 12.9 ms | **−7%** |
| 16 | 220 ms | 226 ms | flat (both configs use identical `CountUniqueFastHalfBoard` path) |

---

## Contributing

1. Fork the repository and create a feature branch (`git checkout -b feature/my-change`).
2. Follow the coding conventions in [`.github/copilot-instructions.md`](.github/copilot-instructions.md).
3. Ensure all tests pass: `dotnet test --configuration Release`.
4. Update `CHANGELOG.md` under `[Unreleased]` before opening a pull request.

---

## License

Distributed under the [MIT License](LICENSE.txt).
