namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }

    public void DisplaySolution(Solution solution)
    {
        if (DataContext is MainViewModel viewModel)
        {
            var positions = solution.QueenPositions.Select((pos, index) =>
                new Position((byte)index, pos)).ToList();

            viewModel.Chessboard.PlaceQueens(positions);
            UpdateSquareVisibility(viewModel);
        }
    }

    private void UpdateSquareVisibility(MainViewModel viewModel)
    {
        foreach (var square in viewModel.Chessboard.Squares)
        {
            square.IsOffscreen = string.IsNullOrEmpty(square.ImagePath);
            if (!square.IsOffscreen)
            {
                // Ensure the element is properly laid out and visible
                var element = GetUIElementForSquare(square);
                if (element != null)
                {
                    element.UpdateLayout();
                }
            }
        }
    }

    private UIElement GetUIElementForSquare(SquareViewModel square)
    {
        // Implement logic to get the corresponding UIElement for the given square
        // This might involve finding the element in the visual tree or using a dictionary to map squares to their UI elements
        return null;
    }
}
