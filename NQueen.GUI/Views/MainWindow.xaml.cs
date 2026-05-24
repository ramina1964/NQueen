using System.Runtime.InteropServices;
using System.Windows.Interop;

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
        LocationChanged += OnLocationChanged;
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

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        if (chessboardPlaceholder.Content is ChessboardUserControl board)
            ApplyFixedLayout(board);
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
                LocationChanged -= OnLocationChanged;
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

        // Capture the initial monitor so the LocationChanged handler
        // does not fire an immediate redundant re-layout.
        _currentMonitor = MonitorFromWindow(new WindowInteropHelper(this).Handle, MonitorDefaultToNearest);

        ApplyFixedLayout(board);
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == _currentMonitor) return;

        _currentMonitor = monitor;
        if (chessboardPlaceholder.Content is ChessboardUserControl board)
            ApplyFixedLayout(board);
    }

    private void ApplyFixedLayout(ChessboardUserControl chessBoard)
    {
        var workArea = GetCurrentMonitorWorkArea();

        // Force a layout pass so row heights are populated.
        UpdateLayout();
        var grid = (Grid)Content;

        // Use the actual rendered Row 0 height (includes HeaderBorder + its Margin="0,0,0,10").
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

    /// <summary>
    /// Returns the work area of the monitor that currently contains this window,
    /// in device-independent pixels (DIPs) at the window's current DPI.
    /// Unlike <see cref="SystemParameters.WorkArea"/>, this is per-monitor aware.
    /// </summary>
    private Rect GetCurrentMonitorWorkArea()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return SystemParameters.WorkArea;

        var hMonitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref info))
            return SystemParameters.WorkArea;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget == null)
            return SystemParameters.WorkArea;

        // M11/M22 is the DIP-to-device-pixel scale at the window's current DPI.
        double dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
        double dpiScaleY = source.CompositionTarget.TransformToDevice.M22;

        return new Rect(
            info.rcWork.Left   / dpiScaleX,
            info.rcWork.Top    / dpiScaleY,
            (info.rcWork.Right  - info.rcWork.Left) / dpiScaleX,
            (info.rcWork.Bottom - info.rcWork.Top)  / dpiScaleY);
    }

    // --- P/Invoke ---

    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int  cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // --- Private fields ---
    private IntPtr _currentMonitor = IntPtr.Zero;
    private bool _disposed = false;
    private readonly IServiceProvider _serviceProvider;
}
