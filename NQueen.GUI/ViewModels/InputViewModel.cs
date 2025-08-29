namespace NQueen.GUI.ViewModels;

// Todo: Resolve the following bug:
// When simulating the second time with the same input, the chessboard is not reset.
public class InputViewModel(SolutionMode solutionMode)
{
    public FluentValidation.Results.ValidationResult ValidateBoardSize(string? boardSizeText)
    {
        boardSizeText ??= string.Empty;

        return _boardSizeValidator.Validate(boardSizeText);
    }

    private readonly BoardSizeValidator _boardSizeValidator = new(solutionMode);
}
