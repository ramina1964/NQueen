namespace NQueen.GUI.Views
{
    public partial class MainView : Window
    {
        public MainView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            Loaded += MainView_Loaded;
            MainViewModel = mainViewModel;
        }

        public MainViewModel MainViewModel { get; set; }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            var board = chessboard;
            var size = (int)Math.Min(board.ActualWidth, board.ActualHeight);
            board.Width = size;
            board.Height = size;
            MainViewModel.SetChessboard(size);
            DataContext = MainViewModel;
        }
    }
}