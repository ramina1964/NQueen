namespace NQueen.Shared.Validation;

public class BoardSizeValidator : AbstractValidator<string>
{
    public BoardSizeValidator(SolutionMode solutionMode)
    {
        (int maxSize, string errorMsg) = GetMaxSizeAndErrorMsg(solutionMode);

        RuleFor(bst => bst)
            .Cascade(CascadeMode.Stop)
            .NotNull().NotEmpty()
            .WithMessage(ErrorMessages.ValueNullOrWhiteSpaceMsg)
            .Must(bst => ParsingUtils.TryParseInt(bst, out _))
            .WithMessage(ErrorMessages.InvalidIntegerError);

        RuleFor(bst => bst)
            .Must(bst => ParsingUtils.ParseIntOrThrow(bst) >= BoardSettings.MinSize)
            .WithMessage(ErrorMessages.SizeTooSmallMsg);

        RuleFor(bst => bst)
            .Must(bst => ParsingUtils.ParseIntOrThrow(bst) <= maxSize)
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
