namespace NQueen.Domain.Utils;

public static class SymmetryPruning
{
    public static List<Memory<int>> GetSymmetricalSolutions(Memory<int> solution)
    {
        var boardSize = solution.Length;

        var symmToMidHorizontal = new Memory<int>(new int[boardSize]);
        var symmToMidVertical = new Memory<int>(new int[boardSize]);
        var symmToMainDiag = new Memory<int>(new int[boardSize]);
        var symmToBiDiag = new Memory<int>(new int[boardSize]);
        var counter90 = new Memory<int>(new int[boardSize]);
        var counter180 = new Memory<int>(new int[boardSize]);
        var counter270 = new Memory<int>(new int[boardSize]);
        var solutionSpan = solution.Span;

        for (var rowIndex = 0; rowIndex < boardSize; rowIndex++)
        {
            var flippedRowIndex = boardSize - rowIndex - 1;
            var flippedColIndex = boardSize - solutionSpan[rowIndex] - 1;

            symmToMidHorizontal.Span[flippedRowIndex] = solutionSpan[rowIndex];
            counter90.Span[flippedColIndex] = symmToMainDiag.Span[solutionSpan[rowIndex]] = rowIndex;

            counter180.Span[flippedRowIndex] =
                symmToMidVertical.Span[rowIndex] = flippedColIndex;

            counter270.Span[solutionSpan[rowIndex]] =
                symmToBiDiag.Span[flippedColIndex] = flippedRowIndex;
        }

        return
        [
            symmToMidVertical,
            symmToMidHorizontal,
            symmToMainDiag,
            symmToBiDiag,
            counter90,
            counter180,
            counter270
        ];
    }

    public static bool IsSymmetrical(
        Memory<int> solution,
        HashSet<Memory<int>> uniqueSolutions)
    {
        // Check if the solution or any of its symmetrical transformations already exists
        foreach (var transformation in GetSymmetricalSolutions(solution))
        {
            if (uniqueSolutions.Contains(transformation))
                return true;
        }

        return false;
    }
}