namespace NQueen.GUI.ViewModels;

public class SolutionViewModel
{
    public SolutionViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        BoardSizeText = MainViewModel.BoardSizeText;
        BoardSize = int.Parse(BoardSizeText);
    }

    public void UpdateGui()
    {
        MainViewModel.ObservableSolutions.Clear();
        MainViewModel.Chessboard?.Squares.Clear();
        MainViewModel.BoardSize = BoardSize;
        MainViewModel.NoOfSolutions = "0";
        MainViewModel.ElapsedTimeInSec = $"{0,0:N1}";
        MainViewModel.MemoryUsage = "0";
        MainViewModel.Chessboard?.CreateSquares(BoardSize, []);
    }

    private MainViewModel MainViewModel { get; }

    private string BoardSizeText { get; }

    private int BoardSize { get; }
}
