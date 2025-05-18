namespace NQueen.GUI.Views;

public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl(MainViewModel mainVm)
    {
        InitializeComponent();
        DataContext = mainVm;
    }
}
