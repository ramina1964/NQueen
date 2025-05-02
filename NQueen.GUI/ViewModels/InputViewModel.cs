namespace NQueen.GUI.ViewModels;

public class InputViewModel : AbstractValidator<MainViewModel>
{
    public InputViewModel() => ValidationRules();

    private void ValidationRules()
    {
        var boardSize = -1;
        _ = RuleFor(vm => vm.BoardSizeText)
            .NotNull().NotEmpty()
            .WithMessage(_ => ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => int.TryParse(bst, out boardSize))
            .WithMessage(_ => ErrorMessages.InvalidIntegerError)
            .Must(bst => BoardSettings.MinSize <= boardSize)
            .WithMessage(_ => ErrorMessages.SizeTooSmallMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForSingleMode)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForUniqueMode)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForAllMode)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => ErrorMessages.SizeTooLargeForAllSolutionsMsg);
    }
}
