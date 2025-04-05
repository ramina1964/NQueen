namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel) : IEventManager
{
    public void SubscribeToSimulationEvents()
    {
        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Register<ProgressValueChangedMessage>(this, OnProgressValueChanged);

        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Register<QueenPlacedMessage>(this, OnQueenPlaced);

        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
        WeakReferenceMessenger.Default.Register<SolutionFoundMessage>(this, OnSolutionFound);
    }

    public void UnsubscribeFromSimulationEvents()
    {
        WeakReferenceMessenger.Default.Unregister<ProgressValueChangedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<QueenPlacedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SolutionFoundMessage>(this);
    }

    public void OnBoardSizeChanged()
    {
        var validationResult = _mainViewModel.InputViewModel.Validate(
            _mainViewModel.Solver,
            _mainViewModel.CommandManager, _mainViewModel);

        _mainViewModel.IsValid = validationResult.IsValid;
        if (!_mainViewModel.IsValid)
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

            if (ValidationHelper.IsBoardSizeFormattedCorrectly(BoardSize))
                _mainViewModel.UpdateGui();
        }
    }

    public void OnSolutionModeChanged(SolutionMode value)
    {
        _mainViewModel.SolutionMode = value;
        var validationResult = _mainViewModel.InputViewModel.Validate(_mainViewModel.Solver, _mainViewModel.CommandManager, _mainViewModel);

        if (!validationResult.IsValid)
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

        if (ValidationHelper.IsBoardSizeFormattedCorrectly(BoardSize))
            _mainViewModel.UpdateGui();
    }

    private void OnProgressValueChanged(object recipient, ProgressValueChangedMessage message)
    {
        _mainViewModel.ProgressValue = message.Value;
        _mainViewModel.ProgressLabel = $"{message.Value} %";
    }

    private void OnQueenPlaced(object recipient, QueenPlacedMessage message)
    {
        var sol = new Solution(message.Value, 1);
        var positions = sol
            .QueenPositions.Where(q => q < _mainViewModel.BoardSize)
            .Select((item, index) => new Position(index, item)).ToList();

        _mainViewModel.Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object recipient, SolutionFoundMessage message)
    {
        var id = _mainViewModel.ObservableSolutions.Count + 1;
        var sol = new Solution(message.Value, id);

        _mainViewModel.NoOfSolutions = $"{int.Parse(_mainViewModel.NoOfSolutions.Replace(" ", "").Replace(",", "")) + 1,0:N0}";

        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (_mainViewModel.ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                _mainViewModel.ObservableSolutions.RemoveAt(0);
            }
            if (_mainViewModel.ObservableSolutions.All(s => s.Id != sol.Id))
            {
                _mainViewModel.ObservableSolutions.Add(sol);
            }
        }));

        _mainViewModel.SelectedSolution = sol;
    }

    public void OnDisplayModeChanged()
    {
        _mainViewModel.InputViewModel.Validate(
            _mainViewModel.Solver,
            _mainViewModel.CommandManager,
            _mainViewModel);

        _mainViewModel.UpdateButtonFunctionality();
    }

    private string BoardSize => _mainViewModel.BoardSize.ToString();

    private readonly MainViewModel _mainViewModel = mainViewModel;
}
