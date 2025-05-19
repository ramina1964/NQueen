namespace NQueen.ViewModelTests.Tests.Main;

public class MainViewModelValidationTests
{
    [Theory]
    [InlineData("8", true, null)]
    [InlineData("0", false, nameof(ErrorMessages.SizeTooSmallMsg))]
    [InlineData("-1", false, nameof(ErrorMessages.SizeTooSmallMsg))]
    [InlineData("abc", false, nameof(ErrorMessages.InvalidIntegerError))]
    [InlineData("", false, nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg))]
    public void BoardSizeText_Validation_ShouldReportErrors(string input, bool isValid,
        string? expectedErrorKey)
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = input;

        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();

        if (isValid)
        {
            errors.Should().BeEmpty();
            mainVm.HasErrors.Should().BeFalse();
        }
        else
        {
            errors.Should().NotBeEmpty();
            mainVm.HasErrors.Should().BeTrue();

            // Check the error message if expectedErrorKey is provided
            if (expectedErrorKey != null)
            {
                var expectedError = expectedErrorKey switch
                {
                    nameof(ErrorMessages.SizeTooSmallMsg) => ErrorMessages.SizeTooSmallMsg,
                    nameof(ErrorMessages.InvalidIntegerError) => ErrorMessages.InvalidIntegerError,
                    nameof(ErrorMessages.ValueNullOrWhiteSpaceMsg) => ErrorMessages.ValueNullOrWhiteSpaceMsg,
                    _ => null
                };
                errors.Should().Contain(expectedError);
            }
        }
    }

    [Fact]
    public void ValidationError_ShouldClear_WhenInputBecomesValid()
    {
        var mainVm = TestHelpers.CreateMainViewModel();
        mainVm.BoardSizeText = "abc";
        var errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().NotBeEmpty();

        mainVm.BoardSizeText = "8";
        errors = mainVm.GetErrors(nameof(mainVm.BoardSizeText)).Cast<string>().ToList();
        errors.Should().BeEmpty();
        mainVm.HasErrors.Should().BeFalse();
    }
}
