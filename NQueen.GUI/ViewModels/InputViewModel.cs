namespace NQueen.GUI.ViewModels;

public class InputViewModel(SolutionMode solutionMode)
{
    /// <summary>
    /// Validates the BoardSizeText using the shared BoardSizeValidator.
    /// </summary>
    /// <param name="boardSizeText">The board size text to validate.</param>
    /// <returns>A FluentValidation.ValidationResult indicating the validation outcome.</returns>
    public FluentValidation.Results.ValidationResult ValidateBoardSize(string boardSizeText) =>
         _boardSizeValidator.Validate(boardSizeText);

    private readonly BoardSizeValidator _boardSizeValidator = new(solutionMode);
}
