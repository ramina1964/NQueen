namespace NQueen.Shared.Validation;

public class BoardSizeValidator : AbstractValidator<string>
{
    public BoardSizeValidator(SolutionMode solutionMode)
    {
        RuleFor(bst => bst)
            .NotNull().NotEmpty()
            .WithMessage(ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => int.TryParse(bst, out _))
            .WithMessage(ErrorMessages.InvalidIntegerError)
            .WithName("BoardSizeText");

        RuleFor(bst => bst)
            .Must(bst =>
            {
                if (int.TryParse(bst, out var boardSize))
                {
                    Debug.WriteLine($"Validating board size: {boardSize}");
                    return boardSize >= BoardSettings.MinSize;
                }
                return false;
            })
            .WithMessage(ErrorMessages.SizeTooSmallMsg)
            .WithName("BoardSizeText");

        RuleFor(bst => bst)
            .Must(bst =>
            {
                if (int.TryParse(bst, out var boardSize))
                {
                    return boardSize <= GetMaxSizeForMode(solutionMode);
                }
                return false;
            })
            .WithMessage(GetErrorMessageForMode(solutionMode))
            .WithName("BoardSizeText");
    }

    private static int GetMaxSizeForMode(SolutionMode mode) => mode switch
    {
        SolutionMode.Single => BoardSettings.MaxSizeForSingleMode,
        SolutionMode.Unique => BoardSettings.MaxSizeForUniqueMode,
        SolutionMode.All => BoardSettings.MaxSizeForAllMode,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static string GetErrorMessageForMode(SolutionMode mode) => mode switch
    {
        SolutionMode.Single => ErrorMessages.SizeTooLargeForSingleMsg,
        SolutionMode.Unique => ErrorMessages.SizeTooLargeForUniqueMsg,
        SolutionMode.All => ErrorMessages.SizeTooLargeForAllMsg,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
