namespace NQueen.GUI.ViewModels;

public class CommandManager : ICommandManager
{
    public CommandManager()
    {
        SimulateCommand = new AsyncRelayCommand(SimulateAsync, CanSimulate);
        CancelCommand = new RelayCommand(Cancel, CanCancel);
        SaveCommand = new RelayCommand(Save, CanSave);
    }

    public void Initialize(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        NotifyCommands();
    }

    public IAsyncRelayCommand SimulateCommand { get; private set; }

    public RelayCommand CancelCommand { get; private set; }

    public RelayCommand SaveCommand { get; private set; }

    private async Task SimulateAsync()
    {
        _mainViewModel.ManageSimulationStatus(SimulationStatus.Started);

        _mainViewModel.UpdateGui();
        _mainViewModel.SimulationResults = await _mainViewModel.Solver.GetResultsAsync(
            _mainViewModel.BoardSize, _mainViewModel.SolutionMode, _mainViewModel.DisplayMode);

        _mainViewModel.ExtractCorrectNoOfSols();
        _mainViewModel.NoOfSolutions = $"{_mainViewModel.SimulationResults.NoOfSolutions,0:N0}";
        _mainViewModel.ElapsedTimeInSec = $"{_mainViewModel.SimulationResults.ElapsedTimeInSec,0:N1}";
        _mainViewModel.SelectedSolution = _mainViewModel.ObservableSolutions.FirstOrDefault();

        // Update memory usage after the simulation process completes
        _mainViewModel.MemoryUsage = MemoryMonitoring.UpdateMemoryUsage();

        _mainViewModel.ManageSimulationStatus(SimulationStatus.Finished);
    }

    private bool CanSimulate() =>
        _mainViewModel != null && _mainViewModel.IsIdle && _mainViewModel.IsValid;

    private void Cancel() => 
        _mainViewModel.Solver.IsSolverCanceled = true;

    private bool CanCancel() =>
        _mainViewModel != null && _mainViewModel.IsSimulating;

    private void Save()
    {
        var results = new ResultPresentation(_mainViewModel.SimulationResults);
        var filePath = results.Write2File(_mainViewModel.SolutionMode);
        var msg = $"Successfully wrote results to: {filePath}";
        MessageBox.Show(msg);
        _mainViewModel.IsIdle = true;
    }

    private bool CanSave() =>
        _mainViewModel != null && _mainViewModel.IsOutputReady;

    public void NotifyCommands()
    {
        SimulateCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
    }

    private MainViewModel _mainViewModel;
}
