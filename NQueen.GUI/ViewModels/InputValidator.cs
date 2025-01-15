namespace NQueen.GUI.ViewModels;

public class InputValidator : AbstractValidator<MainViewModel>
{
    public InputValidator() => ValidationRules();

    private void ValidationRules()
    {
        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => IsBoardSizeFormattedCorrectly(boardSize.ToString()))
            .WithMessage(_ => Messages.InvalidByteError);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= BoardSettings.MinBoardSize)
            .WithMessage(_ => Messages.SizeTooSmallMsg);

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

    private static bool IsBoardSizeFormattedCorrectly(string boardSize) =>
        byte.TryParse(boardSize, out byte result) &&
        byte.MinValue <= result && result <= byte.MaxValue;
}
