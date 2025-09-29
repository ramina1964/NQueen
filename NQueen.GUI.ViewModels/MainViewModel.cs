partial void OnSolutionModeChanged(SolutionMode value)
{
    // If a simulation is running, retain the original (more invasive) reset behavior.
    if (IsSimulating)
    {
        ResetAndValidateSimulationState(solutionMode: value);
        return;
    }

    // Preserve current BoardSizeText and existing squares. Just swap validator.
    var currentSizeText = BoardSizeText;
    InputViewModel = new InputViewModel(value);

    // Re-run validation against the current size.
    ValidateProperty(nameof(BoardSizeText));

    // If size is now invalid for the new mode, do NOT mutate the board (tests rely on this).
    if (!IsValid)
    {
        // Ensure error state (HasErrors) is reflected and commands updated.
        RefreshCommandStates();
        return;
    }

    // Still valid: ensure board matches the (preserved) size only if mismatched.
    if (ParsingUtils.TryParseInt(currentSizeText, out var size))
    {
        if (ChessboardVm != null &&
            (ChessboardVm.Squares.Count == 0 ||
             ChessboardVm.Squares.Count != size * size))
        {
            var dim = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
            // Set dimensions if they were never initialized.
            if (ChessboardVm.WindowWidth <= 0 || ChessboardVm.WindowHeight <= 0)
            {
                ChessboardVm.WindowWidth = 800;
                ChessboardVm.WindowHeight = 800;
            }
            ChessboardVm.CreateSquares(size);
        }
    }

    RefreshCommandStates();
}

private void ResetAndValidateSimulationState(string? boardSizeText = null,
    SolutionMode? solutionMode = null)
{
    Cancel();
    SubscribeToSimulationEvents();

    int? originalSize = null;
    string originalText = BoardSizeText;

    if (solutionMode.HasValue)
    {
        InputViewModel = new InputViewModel(solutionMode.Value);

        // Re-validate current text against new mode BEFORE touching it
        var currentValid = InputViewModel.ValidateBoardSize(originalText).IsValid;
        var visualizationOk = !(DisplayMode == DisplayMode.Visualize &&
                                ParsingUtils.TryParseInt(originalText, out var ov) &&
                                ov > SimulationSettings.MaxVisualizeBoardSize);

        if (currentValid && visualizationOk)
        {
            // Keep existing size; do not reset board yet.
            // We will only re-run validation below.
        }
        else
        {
            // Fall back to default only if truly invalid for new mode
            BoardSizeText = BoardSettings.DefaultBoardSize.ToString();
        }
    }

    ValidateProperty(nameof(BoardSizeText));

    // Only rebuild if the (possibly preserved) size is valid
    if (ValidateAndSetUiState())
    {
        _lastValidBoardSize = ParsingUtils.ParseIntOrThrow(BoardSizeText);
        OnPropertyChanged(nameof(BoardSize));
        var dim = Math.Min(ChessboardVm.WindowWidth, ChessboardVm.WindowHeight);
        ResetChessboard(dim);
    }

    RefreshCommandStates();
}