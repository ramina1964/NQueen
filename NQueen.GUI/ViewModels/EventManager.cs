namespace NQueen.GUI.ViewModels;

public class EventManager(MainViewModel mainViewModel)
{
    public void SubscribeToSimulationEvents()
    {
        mainViewModel.Solver.ProgressValueChanged += OnProgressValueChanged;
        mainViewModel.Solver.QueenPlaced += OnQueenPlaced;
        mainViewModel.Solver.SolutionFound += OnSolutionFound;
    }

    public void UnsubscribeFromSimulationEvents()
    {
        mainViewModel.Solver.ProgressValueChanged -= OnProgressValueChanged;
        mainViewModel.Solver.QueenPlaced -= OnQueenPlaced;
        mainViewModel.Solver.SolutionFound -= OnSolutionFound;
    }

    public void OnBoardSizeChanged()
    {
        var validationResult = mainViewModel.InputViewModel.Validate(mainViewModel);
        mainViewModel.IsValid = validationResult.IsValid;

        if (!mainViewModel.IsValid)
        {
            mainViewModel.IsIdle = false;
            mainViewModel.IsSimulating = false;
            mainViewModel.IsSimulateButtonEnabled = false;
            mainViewModel.ValidationError = validationResult.Errors.First().ErrorMessage;
            mainViewModel.HasValidationError = true;
            mainViewModel.InputViewModel.ErrorMessage = validationResult.Errors.First().ErrorMessage;
            mainViewModel.InputViewModel.IsErrorVisible = true;
        }
        else
        {
            mainViewModel.IsIdle = true;
            mainViewModel.IsSimulating = false;
            mainViewModel.IsOutputReady = false;
            mainViewModel.IsSimulateButtonEnabled = true;
            mainViewModel.ValidationError = string.Empty;
            mainViewModel.HasValidationError = false;
            mainViewModel.InputViewModel.ErrorMessage = string.Empty;
            mainViewModel.InputViewModel.IsErrorVisible = false;
            mainViewModel.UpdateButtonFunctionality();
            mainViewModel.UpdateGui();
        }
    }

    public void OnSolutionModeChanged(SolutionMode value)
    {
        mainViewModel.SolutionMode = value;
        var validationResult = mainViewModel.InputViewModel.Validate(mainViewModel);

        if (validationResult.IsValid == false)
        {
            mainViewModel.ValidationError = validationResult.Errors.First().ErrorMessage;
            mainViewModel.HasValidationError = true;
            mainViewModel.IsValid = false;
            mainViewModel.IsInputValid = false;
            mainViewModel.IsSimulateButtonEnabled = false;
            mainViewModel.InputViewModel.ErrorMessage = validationResult.Errors.First().ErrorMessage;
            mainViewModel.InputViewModel.IsErrorVisible = true;
            mainViewModel.UpdateButtonFunctionality();
            return;
        }

        mainViewModel.ValidationError = string.Empty;
        mainViewModel.HasValidationError = false;
        mainViewModel.IsValid = true;
        mainViewModel.IsInputValid = true;
        mainViewModel.IsSimulateButtonEnabled = true;
        mainViewModel.InputViewModel.ErrorMessage = string.Empty;
        mainViewModel.InputViewModel.IsErrorVisible = false;

        mainViewModel.Initialize(mainViewModel.BoardSize, value, mainViewModel.DisplayMode);
        mainViewModel.UpdateButtonFunctionality();

        // Only update the GUI if the board size is valid
        if (mainViewModel.IsValid)
        {
            mainViewModel.UpdateGui();
        }
    }

    private void OnProgressValueChanged(object sender, ProgressValueChangedEventArgs e)
    {
        mainViewModel.ProgressValue = e.Value;
        mainViewModel.ProgressLabel = $"{e.Value} %";
    }

    private void OnQueenPlaced(object sender, QueenPlacedEventArgs e)
    {
        var sol = new Solution(e.Solution, 1);
        var positions = sol
            .QueenPositions.Where(q => q < BoardSettings.ByteMaxValue)
            .Select((item, index) => new Position((byte)index, item)).ToList();

        mainViewModel.Chessboard?.PlaceQueens(positions);
    }

    private void OnSolutionFound(object sender, SolutionFoundEventArgs e)
    {
        var id = mainViewModel.ObservableSolutions.Count + 1;
        var sol = new Solution(e.Solution, id);

        // Update the total number of solutions
        mainViewModel.NoOfSolutions = $"{int.Parse(mainViewModel.NoOfSolutions.Replace(" ", "").Replace(",", "")) + 1,0:N0}";

        // Limit the number of solutions shown in ObservableSolutions
        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
        {
            if (mainViewModel.ObservableSolutions.Count >= SolutionHelper.MaxNoOfSolutionsInOutput)
            {
                mainViewModel.ObservableSolutions.RemoveAt(0);
            }
            if (mainViewModel.ObservableSolutions.Any(s => s.Id == sol.Id) == false)
            {
                mainViewModel.ObservableSolutions.Add(sol);
            }
        }));

        mainViewModel.SelectedSolution = sol;
        mainViewModel.UpdateSolutionTitle();
    }

    public void OnDisplayModeChanged()
    {
        mainViewModel.InputViewModel.Validate(mainViewModel);
        mainViewModel.UpdateButtonFunctionality();
    }

    private readonly MainViewModel mainViewModel = mainViewModel;
}
