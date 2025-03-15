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
            .Must(boardSize => IsBoardSizeFormattedCorrectly(boardSize.ToString()))
            .WithMessage(_ => Messages.BoardSizeFormatError);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize >= BoardSettings.MinBoardSize)
            .WithMessage(_ => Messages.BoardSizeFormatError);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInSingleSolution)
            .When(vm => vm.SolutionMode == SolutionMode.Single)
            .WithMessage(_ => Messages.SingleSizeOutOfRangeMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInUniqueSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.Unique)
            .WithMessage(_ => Messages.UniqueSizeOutOfRangeMsg);

        _ = RuleFor(vm => vm.BoardSize)
            .Must(boardSize => boardSize <= BoardSettings.MaxBoardSizeInAllSolutions)
            .When(vm => vm.SolutionMode == SolutionMode.All)
            .WithMessage(_ => Messages.AllSizeOutOfRangeMsg);
    }

    // Todo: Remove or change this property.
    public static bool IsBoardSizeFormattedCorrectly(string boardSize)
    {
        return byte.TryParse(boardSize, out byte result) &&
        byte.MinValue <= result && result <= byte.MaxValue;
    }
}
