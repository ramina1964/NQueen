namespace NQueen.GUI.Views;

public partial class SimulationPanelUserControl : UserControl
{
    public SimulationPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        MainViewModel = mainViewModel;
        DataContext = mainViewModel;
    }

    public MainViewModel MainViewModel { get; }
}
