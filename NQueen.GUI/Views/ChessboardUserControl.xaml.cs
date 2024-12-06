namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
