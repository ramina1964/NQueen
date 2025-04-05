namespace NQueen.GUI.ViewModels;

public sealed partial class InputViewModel(InputValidator validator) : ObservableObject
{
    public FluentValidationResult Validate(
        ISolver solver,
        ICommandManager commandManager,
        MainViewModel mainViewModel)
    {
        var context = new ValidationContext<MainViewModel>(mainViewModel);
        var result = _validator.Validate(context);
        ErrorMessage = result.IsValid ? string.Empty : result.Errors.First().ErrorMessage;
        IsErrorVisible = !result.IsValid;
        return result;
    }

    [ObservableProperty]
    private string _boardSizeInput;

    [ObservableProperty]
    private string _errorMessage;

    [ObservableProperty]
    private bool _isErrorVisible;

    private readonly InputValidator _validator = validator ??
        throw new ArgumentNullException(nameof(validator));
}
