namespace NQueen.GUI.ViewModels;

public sealed partial class InputViewModel(InputValidator validator) : ObservableObject
{
    public FluentValidationResult Validate(MainViewModel mainViewModel)
    {
        var result = _validator.Validate(mainViewModel);
        ErrorMessage = result.IsValid ? string.Empty : result.Errors.First().ErrorMessage;
        IsErrorVisible = result.IsValid == false;
        return result;
    }

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _isErrorVisible;

    private readonly InputValidator _validator = validator ??
        throw new ArgumentNullException(nameof(validator));
}
