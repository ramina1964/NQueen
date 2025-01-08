namespace NQueen.GUI.ViewModels;

public interface ICommandManager
{
    IAsyncRelayCommand SimulateCommand { get; }

    RelayCommand CancelCommand { get; }

    RelayCommand SaveCommand { get; }

    void Initialize(MainViewModel viewModel);
}
