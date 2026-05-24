using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace NQueen.GUI.Views;

public partial class MainWindow : Window, IDisposable
{
    // Layout constants
    private const double OuterMargin = 10; // grid Margin on each side
    private const double BoardMargin =  8; // breathing space above/below board
    private const double SafetyPad   = 12; // absorbs 1px border + 2px margin rounding

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
        RefreshLayout(force: true);
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
        if (chessboardPlaceholder.Content is not ChessboardUserControl)
            throw new InvalidOperationException(
                "chessboardPlaceholder.Content is not a ChessboardUserControl.");

        RefreshLayout(force: true);
    }

    private void OnLocationChanged(object? sender, EventArgs e) =>
        RefreshLayout(force: false);

    /// <summary>
    /// Re-applies the fixed layout if the window is on a new monitor (<paramref name="force"/>=false)
    /// or unconditionally (<paramref name="force"/>=true).
    /// Centralises the monitor-detection and layout steps shared by Loaded, LocationChanged and OnDpiChanged.
    /// </summary>
    private void RefreshLayout(bool force)
    {
        if (chessboardPlaceholder.Content is not ChessboardUserControl board)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (!force && monitor == _currentMonitor) return;

        _currentMonitor = monitor;
        ApplyFixedLayout(board);
    }

    private void ApplyFixedLayout(ChessboardUserControl chessBoard)
    {
        // Layout pass so Row 0 ActualHeight is populated before arithmetic.
        UpdateLayout();
        var grid = (Grid)Content;

        var row0Height   = grid.RowDefinitions[0].ActualHeight;
        var chromeHeight = ComputeChromeHeight();
        var boardSize    = ComputeBoardSize(GetCurrentMonitorWorkArea().Height, chromeHeight, row0Height);

        // Set the dynamic board column and the two controls that must fill it.
        grid.ColumnDefinitions[2].Width = new GridLength(boardSize);
        chessBoard.Width    = boardSize;
        chessBoard.Height   = boardSize;
        solutionList.Height = boardSize;

        // Width is intentionally NOT set here — SizeToContent="Width" on the Window
        // lets WPF measure the fixed-width columns (150+10+boardSize+10+400) and add
        // DWM chrome exactly, eliminating any manual chrome arithmetic.
        Height = chromeHeight + OuterMargin * 2 + row0Height + BoardMargin * 2 + boardSize;

        // Keep the ViewModel dimensions in sync with the physical board.
        MainViewModel.ChessboardVm.WindowWidth  = boardSize;
        MainViewModel.ChessboardVm.WindowHeight = boardSize;

        // Only clear and rebuild squares when idle — never interrupt an active simulation.
        if (!MainViewModel.IsSimulating)
            MainViewModel.ResetChessboard(boardSize);
    }

    // --- Layout helpers ---

    /// <summary>Chrome height for a NoResize window: title bar + top and bottom fixed borders.</summary>
    private static double ComputeChromeHeight() =>
        SystemParameters.WindowCaptionHeight
        + SystemParameters.FixedFrameHorizontalBorderHeight * 2;

    /// <summary>
    /// Largest square that fits inside the monitor work area after subtracting
    /// chrome, outer margins, header row and breathing room.
    /// </summary>
    private static double ComputeBoardSize(double workAreaHeight, double chromeHeight, double row0Height)
    {
        var size = Math.Floor(workAreaHeight
                   - chromeHeight
                   - OuterMargin * 2
                   - BoardMargin * 2
                   - row0Height
                   - SafetyPad);
        return Math.Max(200, size);
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
