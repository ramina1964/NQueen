namespace NQueen.Shared.Validation;

public class BoardSizeValidator : AbstractValidator<string>
{
    public BoardSizeValidator(SolutionMode solutionMode)
    {
        (int maxSize, string errorMsg) = GetMaxSizeAndErrorMsg(solutionMode);

        RuleFor(bst => bst)
            .Cascade(CascadeMode.Stop)
            .NotNull().NotEmpty()
            .WithName("BoardSizeText")
            .WithMessage(ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => ParsingUtils.TryParseInt(bst, out _))
            .WithMessage(ErrorMessages.InvalidIntegerError);

        RuleFor(bst => bst)
            .Must(bst => ParsingUtils.ParseIntOrThrow(bst) >= BoardSettings.MinSize)
            .WithName("BoardSizeText")
            .WithMessage(ErrorMessages.SizeTooSmallMsg);

        RuleFor(bst => bst)
            .Must(bst => ParsingUtils.ParseIntOrThrow(bst) <= maxSize)
            .WithName("BoardSizeText")
            .WithMessage(errorMsg);
    }

    private static (int maxSize, string errorMsg) GetMaxSizeAndErrorMsg(SolutionMode solutionMode) =>
        solutionMode switch
        {
            SolutionMode.Single => (
                BoardSettings.MaxSizeForSingleMode, ErrorMessages.SizeTooLargeForSingleModeMsg),

            SolutionMode.Unique => (
                BoardSettings.MaxSizeForUniqueMode, ErrorMessages.SizeTooLargeForUniqueModeMsg),

            SolutionMode.All => (BoardSettings.MaxSizeForAllMode, ErrorMessages.SizeTooLargeForAllModeMsg),
                _ => throw new ArgumentOutOfRangeException(nameof(solutionMode))
        };
}
