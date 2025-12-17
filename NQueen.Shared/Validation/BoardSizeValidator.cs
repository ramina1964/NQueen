namespace NQueen.Shared.Validation;

public class BoardSizeValidator : AbstractValidator<string>
{
    public BoardSizeValidator(SolutionMode solutionMode)
    {
        (int maxSize, string errorMsg) = GetMaxSizeAndErrorMsg(solutionMode);

        // Stop on first failure to avoid multiple messages for a single invalid input
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(bst => bst)
            .Cascade(CascadeMode.Stop)
            .NotNull().NotEmpty()
                .WithName("BoardSizeText")
                .WithMessage(ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => ParsingUtils.TryParseInt(bst, out _))
                .WithName("BoardSizeText")
                .WithMessage(ErrorMessages.InvalidIntegerError);

        // Apply numeric constraints only when parsing succeeds
        RuleFor(bst => bst)
            .Cascade(CascadeMode.Stop)
            .Must(bst => ParsingUtils.TryParseInt(bst, out var v) && v >= BoardSettings.MinSize)
                .WithName("BoardSizeText")
                .WithMessage(ErrorMessages.SizeTooSmallMsg)
            .Must(bst => ParsingUtils.TryParseInt(bst, out var v2) && v2 <= maxSize)
                .WithName("BoardSizeText")
                .WithMessage(errorMsg);
    }

    private static (int maxSize, string errorMsg) GetMaxSizeAndErrorMsg
        (SolutionMode solutionMode) =>
        solutionMode switch
        {
            SolutionMode.Single => (
                BoardSettings.MaxSizeForSingle, ErrorMessages.SizeTooLargeForSingle),

            SolutionMode.Unique => (
                BoardSettings.MaxSizeForUnique, ErrorMessages.SizeTooLargeForUnique),

            SolutionMode.All => (BoardSettings.MaxSizeForAll, ErrorMessages.SizeTooLargeForAll),
            _ => throw new ArgumentOutOfRangeException(nameof(solutionMode))
        };
}
