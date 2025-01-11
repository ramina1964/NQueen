namespace NQueen.GUI.Views;

public partial class SimulationPanelUserControl : UserControl
{
    public SimulationPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        MainViewModel = mainViewModel;
        DataContext = mainViewModel;

        // Ensure CommandManager is initialized
        if (MainViewModel.CommandManager == null)
        {
            throw new ArgumentNullException(nameof(mainViewModel),
                "CommandManager cannot be null");
        }

        MainViewModel.CommandManager.Initialize(MainViewModel);
    }

    public MainViewModel MainViewModel { get; }
}
