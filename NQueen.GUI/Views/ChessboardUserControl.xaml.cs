namespace NQueen.GUI.Views;

public partial class ChessboardUserControl
{
    public ChessboardUserControl()
    {
        InitializeComponent();
    }

    public void DisplaySolution(Solution solution)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.Chessboard.PlaceQueens(solution.Positions);
        }
    }
}
