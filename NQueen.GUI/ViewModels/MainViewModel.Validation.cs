namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count != 0;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.Values.SelectMany(errors => errors).ToList();

        return _errors.TryGetValue(propertyName, out var propertyErrors)
            ? propertyErrors
            : Enumerable.Empty<string>();
    }

    private void ValidateProperty(string propertyName)
    {
        _errors.Remove(propertyName);

        var validationResults = InputViewModel.ValidateBoardSize(BoardSizeText);
        foreach (var error in validationResults.Errors)
        {
            Debug.WriteLine($"Validation Error: {error.PropertyName} - {error.ErrorMessage}");
        }

        var propertyErrors = validationResults.Errors
            .Where(error => error.PropertyName == nameof(BoardSizeText))
            .Select(error => error.ErrorMessage)
            .ToList();

        if (propertyErrors.Count != 0)
        {
            _errors[propertyName] = propertyErrors;
            Debug.WriteLine($"Errors for {propertyName}: {string.Join(", ", propertyErrors)}");
            OnErrorsChanged(propertyName);
        }
    }

    private void OnErrorsChanged(string propertyName) =>
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

    partial void OnBoardSizeTextChanged(string value)
    {
        // Trigger validation
        ValidateProperty(nameof(BoardSizeText));

        // Update IsValid state
        IsValid = InputViewModel.ValidateBoardSize(BoardSizeText).IsValid;

        if (IsValid == false)
        {
            IsIdle = false;
            IsSimulating = false;
            IsOutputReady = false;
        }
        else
        {
            // Notify that BoardSize has changed
            OnPropertyChanged(nameof(BoardSize));
            IsIdle = true;

            // Update the chessboard
            var boardDimension = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            SetChessboard(boardDimension);
        }

        RefreshCommandStates();
    }
}
