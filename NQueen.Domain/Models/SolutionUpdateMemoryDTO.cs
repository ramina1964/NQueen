namespace NQueen.Domain.Models;

public record SolutionUpdateMemoryDTO(
    int BoardSize,
    SolutionMode SolutionMode,
    Memory<int> QueenPositions,
    HashSet<Memory<int>> Solutions);