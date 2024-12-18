namespace NQueen.GUI.Views;

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
        _chessboardViewModel = mainViewModel.Chessboard;
        Loaded += ChessboardUserControl_Loaded;

        // Subscribe to property changes
        mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;
    }

    private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.BoardSize) || e.PropertyName == nameof(MainViewModel.DisplayMode))
        {
            if (DataContext is MainViewModel mainViewModel)
            {
                CreateDynamicGrid(mainViewModel.BoardSize);
            }
        }
    }

    public void CreateDynamicGrid(int boardSize)
    {
        ChessboardGrid.RowDefinitions.Clear();
        ChessboardGrid.ColumnDefinitions.Clear();
        ChessboardGrid.Children.Clear();

        for (int i = 0; i < boardSize; i++)
        {
            ChessboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            ChessboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        if (_chessboardViewModel?.Squares != null)
        {
            foreach (var square in _chessboardViewModel.Squares)
            {
                var border = new Border
                {
                    Background = square.Color,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1)
                };
                Grid.SetRow(border, square.Position.RowNo);
                Grid.SetColumn(border, square.Position.ColumnNo);
                ChessboardGrid.Children.Add(border);
            }
        }
        else
        {
            // Fallback: Create a default alternating grid
            for (int row = 0; row < boardSize; row++)
            {
                for (int col = 0; col < boardSize; col++)
                {
                    var isWheat = (row + col) % 2 == 0;
                    var border = new Border
                    {
                        Background = isWheat ? Brushes.Wheat : Brushes.Brown,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1)
                    };
                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    ChessboardGrid.Children.Add(border);
                }
            }
        }
    }

    private void ChessboardUserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
            CreateDynamicGrid(mainViewModel.BoardSize);
    }

    private ChessboardViewModel _chessboardViewModel;
}
