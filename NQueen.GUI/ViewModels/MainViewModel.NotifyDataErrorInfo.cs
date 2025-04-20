namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count != 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return _errors.Values.SelectMany(errors => errors).ToList();
        }

        return _errors.TryGetValue(propertyName, out var propertyErrors)
            ? propertyErrors
            : Enumerable.Empty<string>();
    }

    private void ValidateProperty(string propertyName)
    {
        // Clear existing errors for the property
        if (_errors.ContainsKey(propertyName))
        {
            _errors.Remove(propertyName);
        }

        // Perform validation using InputViewModel
        var validationResults = InputViewModel.Validate(this);
        var propertyErrors = validationResults.Errors
            .Where(error => error.PropertyName == propertyName)
            .Select(error => error.ErrorMessage)
            .ToList();

        if (propertyErrors.Any())
        {
            _errors[propertyName] = propertyErrors;
        }

        // Notify that errors have changed for the property
        OnErrorsChanged(propertyName);
    }

    private void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    // Example usage: Call this method in property setters or partial methods
    partial void OnBoardSizeTextChanged(string value)
    {
        ValidateProperty(nameof(BoardSizeText));

        if (!HasErrors)
        {
            IsIdle = true;
            IsSimulating = false;
            IsOutputReady = false;

            // Update the BoardSize property
            BoardSize = int.Parse(value);

            // Notify that BoardSize has changed
            OnPropertyChanged(nameof(BoardSize));

            // Update the UI
            UpdateButtonFunctionality();
            UpdateGui();
        }
        else
        {
            IsIdle = false;
            IsSimulating = false;
        }
    }
}
