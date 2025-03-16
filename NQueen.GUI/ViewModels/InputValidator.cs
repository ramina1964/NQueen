namespace NQueen.GUI.ViewModels;

public class InputValidator : AbstractValidator<MainViewModel>
{
    public InputValidator()
    {
        ValidationRules();
    }

    private void ValidationRules()
    {
        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => IsBoardSizeFormattedCorrectly(boardSize.ToString()))
            .WithMessage(_ => Messages.BoardSizeFormatError);

        RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= BoardSettings.MinBoardSize)
            .WithMessage(_ => Messages.BoardSizeFormatError);

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

    public static bool IsBoardSizeFormattedCorrectly(string boardSize)
    {
        return int.TryParse(boardSize, out int result) &&
               result >= BoardSettings.MinBoardSize && result <= BoardSettings.ByteMaxValue;
    }
}
