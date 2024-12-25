namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
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
            UpdateSquareVisibility(viewModel);
        }
    }

    private void UpdateSquareVisibility(MainViewModel viewModel)
    {
        foreach (var square in viewModel.Chessboard.Squares)
        {
            square.IsOffscreen = string.IsNullOrEmpty(square.ImagePath);
        }
    }
}
