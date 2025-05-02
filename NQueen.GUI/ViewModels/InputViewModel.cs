namespace NQueen.GUI.ViewModels;

public class InputViewModel : AbstractValidator<MainViewModel>
{
    public InputViewModel() => ValidationRules();

    private void ValidationRules()
    {
        _ = RuleFor(vm => vm.BoardSizeText)
            .NotNull().NotEmpty()
            .WithMessage(_ => ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => int.TryParse(bst, out _))
            .WithMessage(_ => ErrorMessages.InvalidIntegerError)
            .Must(bst =>
            {
                if (int.TryParse(bst, out var boardSize))
                {
                    return BoardSettings.MinSize <= boardSize;
                }
                return false;
            })
            .WithMessage(_ => ErrorMessages.SizeTooSmallMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(bst => ValidateAndParseBoardSize(bst, BoardSettings.MaxSizeForSingleMode, out _))
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(bst => ValidateAndParseBoardSize(bst, BoardSettings.MaxSizeForUniqueMode, out _))
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(bst => ValidateAndParseBoardSize(bst, BoardSettings.MaxSizeForAllMode, out _))
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForAllSolutionsMsg);
    }

    private static bool ValidateAndParseBoardSize(string? boardSizeText, int maxSize, out int boardSize) =>
        int.TryParse(boardSizeText, out boardSize) && boardSize <= maxSize;
}
