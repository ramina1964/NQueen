namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    private void ValidateProperty(string propertyName)
    {
        _errors.Remove(propertyName);

        var validationResults = InputViewModel.ValidateBoardSize(BoardSizeText);
        foreach (var error in validationResults.Errors)
            Debug.WriteLine($"Validation Error: {error.PropertyName} - {error.ErrorMessage}");

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

    partial void OnBoardSizeTextChanged(string value)
    {
        if (Solver == null)
            return;

        if (ParsingUtils.TryParseInt(value, out var boardSize))
            _lastValidBoardSize = boardSize;

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

        ValidateProperty(nameof(BoardSizeText));

        var maxNoOfSols = SimulationSettings.MaxNoOfSolutionsInOutput;
        SolutionTitle = value switch
        {
            SolutionMode.All => $"All Sols. (Max: {maxNoOfSols})",
            SolutionMode.Unique => $"Unique Sols. (Max: {maxNoOfSols})",
            _ => "Single Solution"
        };

        ValidateProperty(nameof(BoardSizeText));
        OnPropertyChanged(nameof(BoardSizeText));
        OnPropertyChanged(nameof(SolutionTitle));

        // Ensure IsSingleRunning is updated when SolutionMode changes
        IsSingleRunning = value == SolutionMode.Single && IsSimulating;

        if (ValidateAndSetUiState())
        {
            _lastValidBoardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);
            OnPropertyChanged(nameof(BoardSize));
            var boardDimension = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            SetChessboard(boardDimension);
        }
        RefreshCommandStates();
    }
}
