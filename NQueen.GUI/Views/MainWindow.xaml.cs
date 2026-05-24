namespace NQueen.GUI.Views;

public partial class MainWindow : Window, IDisposable
{
    // Layout constants
    private const double OuterMargin = 10; // grid Margin on each side
    private const double BoardMargin =  8; // breathing space above/below board
    private const double SafetyPad   = 12; // absorbs 1px border + 2px margin rounding at bottom of board and listbox

    public MainWindow(MainViewModel mainViewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider
            ?? throw new ArgumentNullException(nameof(serviceProvider));

        DataContext = mainViewModel
            ?? throw new ArgumentNullException(nameof(mainViewModel));

        Loaded += MainView_Loaded;
        MainViewModel = mainViewModel;

        // Resolve and add ChessboardUserControl to the MainWindow
        var chessboard = _serviceProvider.GetRequiredService<ChessboardUserControl>();
        chessboard.DataContext = MainViewModel;
        chessboardPlaceholder.Content = chessboard;

        // Resolve and add InputPanelUserControl to the MainWindow
        var inputPanel = _serviceProvider.GetRequiredService<InputPanelUserControl>();
        inputPanel.DataContext = MainViewModel;
        inputPanelPlaceHolder.Content = inputPanel;

        // Resolve and add SimulationPanelUserControl to the MainWindow
        var simulationPanel = _serviceProvider.GetRequiredService<SimulationPanelUserControl>();
        simulationPanel.DataContext = MainViewModel;
        simulationPanelPlaceHolder.Content = simulationPanel;
    }

    public MainViewModel MainViewModel { get; set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (_disposed == false)
        {
            Dispose(true);
            _disposed = true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (MainViewModel != null)
            {
                MainViewModel.Dispose();
                Loaded -= MainView_Loaded;
                MainViewModel = null!;
            }
        }

        _disposed = true;
    }

    private void MainView_Loaded(object sender, RoutedEventArgs e)
    {
        if (chessboardPlaceholder.Content is not ChessboardUserControl board)
            throw new InvalidOperationException(
                "chessboardPlaceholder.Content is not a ChessboardUserControl.");

        ApplyFixedLayout(board);
    }

    private void ApplyFixedLayout(ChessboardUserControl chessBoard)
    {
        var workArea = SystemParameters.WorkArea;

        // Force a layout pass so row heights are populated.
        UpdateLayout();
        var grid = (Grid)Content;

        // Use the actual rendered Row 0 height (includes HeaderBorder + its Margin="0,0,0,10").
        // HeaderBorder.ActualHeight alone excludes the margin, under-counting by 10px and
        // squeezing Row 1 — which was clipping the bottom border of the board and listbox.
        var row0Height = grid.RowDefinitions[0].ActualHeight;

        // For a NoResize window the chrome is: title bar + fixed (non-resize) border top + bottom.
        var chromeHeight = SystemParameters.WindowCaptionHeight
                         + SystemParameters.FixedFrameHorizontalBorderHeight * 2;

        // boardSize fills everything that is not chrome, header, outer margins or breathing room.
        var boardSize = Math.Floor(workArea.Height
                        - chromeHeight
                        - OuterMargin * 2
                        - BoardMargin * 2
                        - row0Height
                        - SafetyPad);
        boardSize = Math.Max(200, boardSize);

        // Column 2 is exactly boardSize wide — no slack, no gaps.
        grid.ColumnDefinitions[2].Width = new GridLength(boardSize);

        // Size board and solution list.
        chessBoard.Width    = boardSize;
        chessBoard.Height   = boardSize;
        solutionList.Height = boardSize;

        // Set window size explicitly so nothing is approximated.
        // Height = chrome + outer margins + header + breathing + board
        Width  = OuterMargin + 150 + 10 + boardSize + 10 + 400 + OuterMargin;
        Height = chromeHeight
               + OuterMargin + row0Height + BoardMargin
               + boardSize
               + BoardMargin + OuterMargin;

        // Pass exact dimensions to the ViewModel.
        MainViewModel.ChessboardVm.WindowWidth  = boardSize;
        MainViewModel.ChessboardVm.WindowHeight = boardSize;
        MainViewModel.ResetChessboard(boardSize);
    }

    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
