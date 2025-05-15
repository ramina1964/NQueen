namespace NQueen.GUI.Views;

public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
