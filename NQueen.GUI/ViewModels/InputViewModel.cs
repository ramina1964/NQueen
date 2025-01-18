namespace NQueen.GUI.ViewModels;

public sealed partial class InputViewModel : ObservableObject
{
    private readonly InputValidator _validator;

    public InputViewModel(InputValidator validator)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public FluentValidationResult Validate(MainViewModel mainViewModel)
    {
        var result = _validator.Validate(mainViewModel);
        ErrorMessage = result.IsValid ? string.Empty : result.Errors.First().ErrorMessage;
        IsErrorVisible = !result.IsValid;
        return result;
    }

    [ObservableProperty]
    private string errorMessage;

    [ObservableProperty]
    private bool isErrorVisible;
}
