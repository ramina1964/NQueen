namespace NQueen.GUI.ViewModels;

public class InputViewModel(SolutionMode solutionMode)
{
    public FluentValidation.Results.ValidationResult ValidateBoardSize(string? boardSizeText)
    {
        boardSizeText ??= string.Empty;

        return _boardSizeValidator.Validate(boardSizeText);
    }

    // --- Private Fields ---
    private readonly BoardSizeValidator _boardSizeValidator = new(solutionMode);
}
