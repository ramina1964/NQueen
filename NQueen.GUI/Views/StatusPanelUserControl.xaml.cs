namespace NQueen.GUI.Views;

public partial class StatusPanelUserControl : UserControl
{
    public StatusPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
