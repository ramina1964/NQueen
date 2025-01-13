namespace NQueen.GUI.ViewModels;

public class InputViewModel : ObservableObject
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

    private string _errorMessage;

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private bool _isErrorVisible;

    public bool IsErrorVisible
    {
        get => _isErrorVisible;
        set => SetProperty(ref _isErrorVisible, value);
    }

    private readonly InputValidator _validator;
}
