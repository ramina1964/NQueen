namespace NQueen.GUI.ViewModels;

public class InputValidator : AbstractValidator<MainViewModel>
{
    public InputValidator()
    {
        ValidationRules();
    }

    private void ValidationRules()
    {
        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= BoardSettings.MinBoardSize)
            .WithMessage(_ => Messages.SizeTooSmallMsg)
            .Must(boardSize => boardSize <= BoardSettings.ByteMaxValue)
            .WithMessage(_ => Messages.InvalidSByteError);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeForSingleSolution)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Messages.SizeTooLargeForSingleSolutionMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeForUniqueSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Messages.SizeTooLargeForUniqueSolutionsMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeForAllSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Messages.SizeTooLargeForAllSolutionsMsg);
    }
}
