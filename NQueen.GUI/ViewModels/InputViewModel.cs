namespace NQueen.GUI.ViewModels;

public class InputViewModel : AbstractValidator<MainViewModel>
{
    public InputViewModel() => ValidationRules();

    private void ValidationRules()
    {
        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= Utility.MinBoardSize)
            .WithMessage(_ => Utility.SizeTooSmallMsg)
            .Must(boardSize => boardSize <= Utility.ByteMaxValue)
            .WithMessage(_ => Utility.InvalidSByteError);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= Utility.MaxBoardSizeForSingleSolution)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Utility.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= Utility.MaxBoardSizeForUniqueSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Utility.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= Utility.MaxBoardSizeForAllSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Utility.SizeTooLargeForAllSolutionsMsg);
    }
}
