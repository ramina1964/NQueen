namespace NQueen.GUI.ViewModels;

public class InputValidator : AbstractValidator<MainViewModel>
{
    public InputValidator() => ValidationRules();

    private void ValidationRules()
    {
        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => ValidationHelper.IsBoardSizeFormattedCorrectly(boardSize.ToString()))
            .WithMessage(_ => Messages.SizeOutOfRangeError);

        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= BoardSettings.MinBoardSize)
            .WithMessage(_ => Messages.SizeOutOfRangeError);

        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInSingleSolution)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Messages.SingleSizeOutOfRangeMsg);

        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInUniqueSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Messages.UniqueSizeOutOfRangeMsg);

        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInAllSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Messages.AllSizeOutOfRangeMsg);
    }
}
