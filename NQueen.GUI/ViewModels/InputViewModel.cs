namespace NQueen.GUI.ViewModels;

public sealed partial class InputViewModel : ObservableObject
{
    public InputViewModel()
    {
        _validator = new InputValidator();
    }

    public FluentValidation.Results.ValidationResult Validate(MainViewModel mainViewModel)
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

    private readonly InputValidator _validator;
}
