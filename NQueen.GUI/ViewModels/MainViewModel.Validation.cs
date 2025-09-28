namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    public string? BoardSizeError =>
        GetErrors(nameof(BoardSizeText)).OfType<string>().FirstOrDefault();

    private void ValidateProperty(string propertyName)
    {
        if (propertyName != nameof(BoardSizeText))
            return;

        _errors.Remove(propertyName);

        if (string.IsNullOrWhiteSpace(BoardSizeText))
        {
            AddBoardSizeError(ErrorMessages.ValueNullOrWhiteSpaceMsg);
            FinalizeBoardSizeValidation(propertyName);
            return;
        }

        var validationResults = InputViewModel.ValidateBoardSize(BoardSizeText);
        var propertyErrors = validationResults.Errors
            .Where(e => e.PropertyName == nameof(BoardSizeText))
            .Select(e => e.ErrorMessage)
            .ToList();

        foreach (var err in propertyErrors)
            AddBoardSizeError(err);

        // Visualization constraint
        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize)
            && DisplayMode == DisplayMode.Visualize
            && boardSize > SimulationSettings.MaxVisualizeBoardSize)
        {
            AddBoardSizeError(ErrorMessages.VisualizeSizeTooLarge);
        }

        FinalizeBoardSizeValidation(propertyName);
    }

    private void AddBoardSizeError(string message)
    {
        if (_errors.TryGetValue(nameof(BoardSizeText), out var list) == false)
        {
            list = new List<string>();
            _errors[nameof(BoardSizeText)] = list;
        }
        if (list.Contains(message) == false)
            list.Add(message);
    }

    private void FinalizeBoardSizeValidation(string propertyName)
    {
        if (_errors.TryGetValue(propertyName, out var errs) && errs.Count > 0)
        {
            OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(BoardSizeError));
            IsValid = false;
            InvalidateVisualizationState();
        }
        else
        {
            // No errors -> mark valid and re-enable commands
            IsValid = true;
            OnErrorsChanged(propertyName);
            OnPropertyChanged(nameof(BoardSizeError));
            RefreshCommandStates();
        }
    }

    private void InvalidateVisualizationState()
    {
        ObservableSolutions.Clear();
        SelectedSolution = null!;
        ChessboardVm?.ClearImages();
        NoOfSolutions = "0";
        IsOutputReady = false;
        IsSimulating = false;
        RefreshCommandStates();
    }

    // Clears ONLY the visualization constraint error if the condition is no longer violated.
    private void ClearVisualizationConstraintIfSatisfied()
    {
        if (_errors.TryGetValue(nameof(BoardSizeText), out var list) == false || list.Count == 0)
            return;

        if (ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) == false)
            return;

        // Condition satisfied (either not visualizing, or within allowed size)
        if (DisplayMode != DisplayMode.Visualize ||
            boardSize <= SimulationSettings.MaxVisualizeBoardSize)
        {
            if (list.Remove(ErrorMessages.VisualizeSizeTooLarge))
            {
                if (list.Count == 0)
                {
                    _errors.Remove(nameof(BoardSizeText));
                    IsValid = InputViewModel.ValidateBoardSize(BoardSizeText).IsValid;
                }
                OnErrorsChanged(nameof(BoardSizeText));
                OnPropertyChanged(nameof(BoardSizeError));
                RefreshCommandStates();
            }
        }
    }

    private bool ValidateAndSetUiState(bool updateOutputReady = true)
    {
        if (string.IsNullOrWhiteSpace(BoardSizeText))
        {
            IsValid = false;
            InvalidateVisualizationState();
            if (updateOutputReady) IsOutputReady = false;
            return false;
        }

        var valid = InputViewModel.ValidateBoardSize(BoardSizeText).IsValid;

        if (valid &&
            DisplayMode == DisplayMode.Visualize &&
            ParsingUtils.TryParseInt(BoardSizeText, out var boardSize) &&
            boardSize > SimulationSettings.MaxVisualizeBoardSize)
        {
            AddBoardSizeError(ErrorMessages.VisualizeSizeTooLarge);
            OnErrorsChanged(nameof(BoardSizeText));
            OnPropertyChanged(nameof(BoardSizeError));
            valid = false;
        }
        else
        {
            // If visualization constraint is no longer broken, clear that specific error
            ClearVisualizationConstraintIfSatisfied();
        }

        IsValid = valid;

        if (!IsValid)
        {
            InvalidateVisualizationState();
            if (updateOutputReady) IsOutputReady = false;
            return false;
        }

        IsIdle = true;
        IsSimulating = false;
        if (updateOutputReady)
            IsOutputReady = false;

        RefreshCommandStates();
        return true;
    }

    partial void OnBoardSizeTextChanged(string value) =>
        ResetAndValidateSimulationState(boardSizeText: value);

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        var currentBoardSizeText = BoardSizeText;
        ResetAndValidateSimulationState(solutionMode: value);
        BoardSizeText = currentBoardSizeText;
    }

    private void ResetAndValidateSimulationState(string? boardSizeText = null,
        SolutionMode? solutionMode = null)
    {
        Cancel();
        SubscribeToSimulationEvents();
        ResetSimulationState();

        if (solutionMode.HasValue)
        {
            InputViewModel = new InputViewModel(solutionMode.Value);
            BoardSizeText = BoardSettings.DefaultBoardSize.ToString();
        }

        if (string.IsNullOrEmpty(boardSizeText) == false)
        {
            if (ParsingUtils.TryParseInt(boardSizeText, out var boardSize))
                _lastValidBoardSize = boardSize;
            ValidateProperty(nameof(BoardSizeText));
        }
        else
        {
            ValidateProperty(nameof(BoardSizeText));
        }

        if (ValidateAndSetUiState())
        {
            _lastValidBoardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);
            OnPropertyChanged(nameof(BoardSize));
            var boardDimension = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            ResetChessboard(boardDimension);
        }

        RefreshCommandStates();
    }

    private void ResetSimulationState()
    {
        ObservableSolutions.Clear();
        SelectedSolution = null!;
        ProgressValue = 0.0;
        ProgressLabel = "0%";
        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0,0:N1}";
        MemoryUsage = "0";
        IsOutputReady = false;
        IsSimulating = false;

        if (ChessboardVm != null)
        {
            var boardDimension = Math.Min(
                ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            ResetChessboard(boardDimension);
        }
    }

    private void OnErrorsChanged(string propertyName) =>
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}
