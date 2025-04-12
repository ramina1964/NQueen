namespace NQueen.GUI.ViewModels;

public class InputViewModel : AbstractValidator<MainViewModel>
{
    public InputViewModel() => ValidationRules();

    private void ValidationRules()
    {
        var boardSize = -1;
        _ = RuleFor(vm => vm.BoardSizeText)
            .NotNull().NotEmpty()
            .WithMessage(_ => Messages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => int.TryParse(bst, out boardSize))
            .WithMessage(_ => Messages.InvalidSIntegerError)
            .Must(bst => BoardSettings.MinSize <= boardSize)
            .WithMessage(_ => Messages.SizeTooSmallMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForSingleMode)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Messages.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForUniqueMode)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Messages.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= BoardSettings.MaxSizeForAllMode)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Messages.SizeTooLargeForAllSolutionsMsg);
    }
}
