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
        if (Solver == null)
            return;

        ValidateProperty(nameof(BoardSizeText));
        if (ValidateAndSetUiState())
        {
            _lastValidBoardSize = ParsingUtils.ParseIntOrThrow(value);
            OnPropertyChanged(nameof(BoardSize));
            var boardDimension = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            SetChessboard(boardDimension);
        }
        RefreshCommandStates();
    }

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        if (Solver == null)
            return;

        InputViewModel = new InputViewModel(value);

        var maxNoOfSols = SimulationSettings.MaxNoOfSolutionsInOutput;
        SolutionTitle = (value == SolutionMode.All)
            ? $"All Sols. (Max: {maxNoOfSols})"
            : (value == SolutionMode.Unique) ? $"Unique Sols. (Max: {maxNoOfSols})"
            : "Single Solution";

        ValidateProperty(nameof(BoardSizeText));
        OnPropertyChanged(nameof(BoardSizeText));
        OnPropertyChanged(nameof(SolutionTitle));

        if (!ValidateAndSetUiState())
            return;

        UpdateUiState();
    }

    private bool ValidateAndSetUiState(bool updateOutputReady = true)
    {
        IsValid = InputViewModel.ValidateBoardSize(BoardSizeText).IsValid;
        if (!IsValid)
        {
            IsIdle = false;
            IsSimulating = false;
            if (updateOutputReady)
                IsOutputReady = false;
            return false;
        }

        IsIdle = true;
        IsSimulating = false;
        
        if (updateOutputReady)
            IsOutputReady = false;

        return true;
    }
}
