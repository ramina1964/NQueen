namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel) : IEventManager
{
    public void SubscribeToSimulationEvents()
    {
        _mainViewModel.Solver.ProgressValueChanged += OnProgressValueChanged;
        _mainViewModel.Solver.QueenPlaced += OnQueenPlaced;
        _mainViewModel.Solver.SolutionFound += OnSolutionFound;
    }

    public void UnsubscribeFromSimulationEvents()
    {
        _mainViewModel.Solver.ProgressValueChanged -= OnProgressValueChanged;
        _mainViewModel.Solver.QueenPlaced -= OnQueenPlaced;
        _mainViewModel.Solver.SolutionFound -= OnSolutionFound;
    }

    public void OnBoardSizeChanged()
    {
        var validationResult = _mainViewModel.InputViewModel.Validate(_mainViewModel);
        _mainViewModel.IsValid = validationResult.IsValid;

        if (_mainViewModel.IsValid == false)
        {
            _mainViewModel.IsIdle = false;
            _mainViewModel.IsSimulating = false;
            _mainViewModel.IsSimulateButtonEnabled = false;
            _mainViewModel.ValidationError = validationResult.Errors.First().ErrorMessage;
            _mainViewModel.HasValidationError = true;
            _mainViewModel.InputViewModel.ErrorMessage = validationResult.Errors.First().ErrorMessage;
            _mainViewModel.InputViewModel.IsErrorVisible = true;
        }
        else
        {
            _mainViewModel.IsIdle = true;
            _mainViewModel.IsSimulating = false;
            _mainViewModel.IsOutputReady = false;
            _mainViewModel.IsSimulateButtonEnabled = true;
            _mainViewModel.ValidationError = string.Empty;
            _mainViewModel.HasValidationError = false;
            _mainViewModel.InputViewModel.ErrorMessage = string.Empty;
            _mainViewModel.InputViewModel.IsErrorVisible = false;
            _mainViewModel.UpdateButtonFunctionality();

            // Only update the GUI if the board size is within the valid range
            if (ValidationHelper.IsBoardSizeFormattedCorrectly(BoardSize))
                _mainViewModel.UpdateGui();
        }
    }

    public void OnSolutionModeChanged(SolutionMode value)
    {
        _mainViewModel.SolutionMode = value;
        var validationResult = _mainViewModel.InputViewModel.Validate(_mainViewModel);

        if (validationResult.IsValid == false)
        {
            _mainViewModel.ValidationError = validationResult.Errors.First().ErrorMessage;
            _mainViewModel.HasValidationError = true;
            _mainViewModel.IsValid = false;
            _mainViewModel.IsSimulateButtonEnabled = false;
            _mainViewModel.InputViewModel.ErrorMessage = validationResult.Errors.First().ErrorMessage;
            _mainViewModel.InputViewModel.IsErrorVisible = true;
            _mainViewModel.UpdateButtonFunctionality();
            return;
        }

        _mainViewModel.ValidationError = string.Empty;
        _mainViewModel.HasValidationError = false;
        _mainViewModel.IsValid = true;
        _mainViewModel.IsSimulateButtonEnabled = true;
        _mainViewModel.InputViewModel.ErrorMessage = string.Empty;
        _mainViewModel.InputViewModel.IsErrorVisible = false;

        _mainViewModel.Initialize(_mainViewModel.BoardSize, value, _mainViewModel.DisplayMode);
        _mainViewModel.UpdateButtonFunctionality();

        // Only update the GUI if the board size is within the valid range
        if (ValidationHelper.IsBoardSizeFormattedCorrectly(BoardSize))
            _mainViewModel.UpdateGui();
    }

    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e)
    {
        _mainViewModel.ProgressValue = e.Value;
        _mainViewModel.ProgressLabel = $"{e.Value} %";
    }

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution(e.Solution, 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.IntMaxValue)
            .Select((item, index) => new Position(index, item)).ToList();

        _mainViewModel.Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = _mainViewModel.ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        _mainViewModel.NoOfSolutions = $"{int.Parse(_mainViewModel.NoOfSolutions.Replace(" ", "").Replace(",", "")) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (_mainViewModel.ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                _mainViewModel.ObservableSolutions.RemoveAt(0);
            }
            if (_mainViewModel.ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                _mainViewModel.ObservableSolutions.Add(sol);
            }
        }));

        _mainViewModel.SelectedSolution = sol;
    }

    public void OnDisplayModeChanged()
    {
        _mainViewModel.InputViewModel.Validate(_mainViewModel);
        _mainViewModel.UpdateButtonFunctionality();
    }

    private readonly MainViewModel _mainViewModel = mainViewModel;

    private string BoardSize => _mainViewModel.BoardSize.ToString();
}
