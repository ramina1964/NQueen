namespace NQueen.GUI.ViewModels;

public class InputViewModel : AbstractValidator<MainViewModel>
{
    public InputViewModel() => ValidationRules();

    private void ValidationRules()
    {
        byte boardSize = Utility.ByteMaxValue;
        _ = RuleFor(vm => vm.BoardSizeText)
            .NotNull().NotEmpty()
            .WithMessage(_ => Utility.ValueNullOrWhiteSpaceMsg)
            .Must(bst => byte.TryParse(bst, out boardSize))
            .WithMessage(_ => Utility.InvalidSByteError)
            .Must(bst => Utility.MinBoardSize <= boardSize)
            .WithMessage(_ => Utility.SizeTooSmallMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= Utility.MaxBoardSizeForSingleSolution)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Utility.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= Utility.MaxBoardSizeForUniqueSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Utility.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSizeText)
            .Must(_ => boardSize <= Utility.MaxBoardSizeForAllSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Utility.SizeTooLargeForAllSolutionsMsg);
    }
}
