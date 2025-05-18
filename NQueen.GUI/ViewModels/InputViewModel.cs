namespace NQueen.GUI.ViewModels;

public class InputViewModel(SolutionMode solutionMode)
{
    public FluentValidation.Results.ValidationResult ValidateBoardSize(string boardSizeText) =>
         _boardSizeValidator.Validate(boardSizeText);

    private readonly BoardSizeValidator _boardSizeValidator = new(solutionMode);
}
