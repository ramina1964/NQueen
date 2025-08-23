namespace NQueen.Domain.Models;

public record SolutionUpdateDTO(
    int BoardSize,
    SolutionMode SolutionMode,
    int[] QueenPositions,
    HashSet<int[]> Solutions);
