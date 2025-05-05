

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
            .Must(bst =>
            {
                if (int.TryParse(bst, out var boardSize))
                {
                    return BoardSettings.MinSize <= boardSize;
                }
                return false;
            })
            .WithMessage(ErrorMessages.SizeTooSmallMsg);

        RuleFor(bst => bst)
            .Must(bst => ValidateAndParseBoardSize(bst, GetMaxSizeForMode(solutionMode), out _))
            .WithMessage(GetErrorMessageForMode(solutionMode));
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
        SolutionMode.Single => ErrorMessages.SizeTooLargeForSingleSolutionMsg,
        SolutionMode.Unique => ErrorMessages.SizeTooLargeForUniqueSolutionsMsg,
        SolutionMode.All => ErrorMessages.SizeTooLargeForAllSolutionsMsg,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static bool ValidateAndParseBoardSize(string? boardSizeText, int maxSize, out int boardSize) =>
        int.TryParse(boardSizeText, out boardSize) && boardSize <= maxSize;
}
