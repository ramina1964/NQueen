namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    public string? BoardSizeError =>
        GetErrors(nameof(BoardSizeText)).OfType<string>().FirstOrDefault();

    // Ensures that after a cancellation we revalidate and, if the AsyncRelayCommand
    // is stuck in a running state, we rebuild it so the Simulate button re-enables.
    private void HandlePostCancel()
    {
        if (SimulateCommand is AsyncRelayCommand arc && arc.IsRunning)
        {
            SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
            OnPropertyChanged(nameof(SimulateCommand));
        }
        ValidateProperty(nameof(BoardSizeText));
        if (IsValid)
        {
            IsIdle = true;
            IsInInputMode = true;
            IsSimulating = false;
            IsOutputReady = false;
        }
        RefreshCommandStates();
    }

    private void ValidateProperty(string propertyName)
    {
        if (propertyName != nameof(BoardSizeText)) return;
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
        foreach (var err in propertyErrors) AddBoardSizeError(err);
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
        if (!list.Contains(message)) list.Add(message);
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

    private void ClearVisualizationConstraintIfSatisfied()
    {
        if (!_errors.TryGetValue(nameof(BoardSizeText), out var list) || list.Count == 0) return;
        if (!ParsingUtils.TryParseInt(BoardSizeText, out var boardSize)) return;
        if (DisplayMode != DisplayMode.Visualize || boardSize <= SimulationSettings.MaxVisualizeBoardSize)
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
        if (valid && DisplayMode == DisplayMode.Visualize &&
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
        if (updateOutputReady) IsOutputReady = false;
        RefreshCommandStates();
        return true;
    }

    partial void OnBoardSizeTextChanged(string value)
    {
        ResetAndValidateSimulationState(boardSizeText: value);
        AutoAdjustParallel();
    }

    partial void OnSolutionModeChanged(SolutionMode value)
    {
        var previousBoardSizeText = BoardSizeText;
        ResetAndValidateSimulationState(solutionMode: value);
        if (!string.IsNullOrWhiteSpace(previousBoardSizeText) &&
            ParsingUtils.TryParseInt(previousBoardSizeText, out var prevSize))
        {
            var validationResult = InputViewModel.ValidateBoardSize(previousBoardSizeText);
            bool validForNewMode = validationResult.IsValid;
            bool visualizationOk = DisplayMode != DisplayMode.Visualize || prevSize <= SimulationSettings.MaxVisualizeBoardSize;
            if (validForNewMode && visualizationOk && previousBoardSizeText != BoardSizeText)
                BoardSizeText = previousBoardSizeText;
        }
        AutoAdjustParallel();
        OnPropertyChanged(nameof(ResultLabel));
        OnPropertyChanged(nameof(SelectedStorageMode));
        ApplyStorageModesToSolver();
    }

    partial void OnDisplayModeChanged(DisplayMode value)
    {
        ValidateProperty(nameof(BoardSizeText));
        AutoAdjustParallel();
    }

    private void ResetAndValidateSimulationState(string? boardSizeText = null, SolutionMode? solutionMode = null)
    {
        Cancel();
        SubscribeToSimulationEvents();
        ResetSimulationState();
        if (solutionMode.HasValue)
        {
            var oldText = BoardSizeText;
            InputViewModel = new InputViewModel(solutionMode.Value);
            if (string.IsNullOrWhiteSpace(oldText))
                BoardSizeText = BoardSettings.DefaultBoardSize.ToString();
            else
                ValidateProperty(nameof(BoardSizeText));
        }
        if (!string.IsNullOrEmpty(boardSizeText))
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
        MemoryConsumption = "0";
        IsOutputReady = false;
        IsSimulating = false;
        if (ChessboardVm != null)
        {
            var boardDimension = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            ResetChessboard(boardDimension);
        }
    }

    private void OnErrorsChanged(string propertyName) =>
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}
