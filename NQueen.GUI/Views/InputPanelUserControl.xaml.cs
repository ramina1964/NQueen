namespace NQueen.GUI.Views;

// Todo: Entering a valid after an invalid board size, causes the red boarder hold itself around
//       the board size textbox.
public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl(MainViewModel mainVm)
    {
        InitializeComponent();
        DataContext = mainVm;
    }
}
