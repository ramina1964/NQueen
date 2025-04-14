namespace NQueen.GUI.Views;

public partial class SimulationPanelUserControl : UserControl
{
    public SimulationPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
