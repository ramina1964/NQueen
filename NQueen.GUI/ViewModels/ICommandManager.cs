namespace NQueen.GUI.ViewModels;


// Todo: Consider adding ICommand from CommunityToolkit.Mvvm, instead
public interface ICommandManager
{
    IAsyncRelayCommand SimulateCommand { get; }

    RelayCommand CancelCommand { get; }

    RelayCommand SaveCommand { get; }

    void Initialize(MainViewModel viewModel);
}
