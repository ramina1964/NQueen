namespace NQueen.GUI.ViewModels;

public sealed partial class MainViewModel : ObservableObject, INotifyDataErrorInfo
{
    public string? BoardSizeError =>
        GetErrors(nameof(BoardSizeText)).OfType<string>().FirstOrDefault();

    private void ValidateProperty(string propertyName)
    {
        _errors.Remove(propertyName);
        var validationResults = InputViewModel.ValidateBoardSize(BoardSizeText);;

        var propertyErrors = validationResults.Errors
            .Where(error => error.PropertyName == nameof(BoardSizeText))
            .Select(error => error.ErrorMessage)
            .ToList();

        if (propertyErrors.Count != 0)
        {
            _errors[propertyName] = propertyErrors;
            OnErrorsChanged(propertyName);
        }
        else
        {
            // Notify UI that errors have been cleared
            OnErrorsChanged(propertyName);
        }

        // Notify UI that BoardSizeError may have changed
        if (propertyName == nameof(BoardSizeText))
            OnPropertyChanged(nameof(BoardSizeError));
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

    partial void OnBoardSizeTextChanged(string value) =>
        ResetAndValidateSimulationState(boardSizeText: value);

    partial void OnSolutionModeChanged(SolutionMode value) =>
        ResetAndValidateSimulationState(solutionMode: value);

    private void ResetAndValidateSimulationState(string? boardSizeText = null,
        SolutionMode? solutionMode = null)
    {
        Cancel();
        SubscribeToSimulationEvents();
        ResetSimulationState();

        // Update InputViewModel if SolutionMode is provided
        if (solutionMode.HasValue)
        {
            InputViewModel = new InputViewModel(solutionMode.Value);
            BoardSizeText = BoardSettings.DefaultBoardSize.ToString();
        }

        // Validate the board size text
        if (!string.IsNullOrEmpty(boardSizeText))
        {
            if (ParsingUtils.TryParseInt(boardSizeText, out var boardSize))
                _lastValidBoardSize = boardSize;

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

        // Use DefaultSolutionFormatter to avoid null formatter
        var defaultFormatter = new DefaultSolutionFormatter();

        // Provide a valid queenPositions array to avoid exceptions
        SelectedSolution = null!;

        ProgressValue = 0.0;
        ProgressLabel = "0%";
        NoOfSolutions = "0";
        ElapsedTimeInSec = $"{0,0:N1}";
        MemoryUsage = "0";
        IsOutputReady = false;
        IsSimulating = false;

        // Reset the chessboard as part of simulation state reset
        if (ChessboardVm != null)
        {
            var boardDimension = Math.Min(
                ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);

            ResetChessboard(boardDimension);
        }
    }
}
