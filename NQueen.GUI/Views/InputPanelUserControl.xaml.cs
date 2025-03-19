namespace NQueen.GUI.Views;

public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        DataContext = _mainViewModel;
    }

    private void TxtBoardSize_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (InputValidator.IsBoardSizeFormattedCorrectly(textBox.Text) == false)
            {
                _mainViewModel.InputViewModel.ErrorMessage = Messages.SizeFormatError;
                _mainViewModel.InputViewModel.IsErrorVisible = true;
                _mainViewModel.IsValid = false;
                _mainViewModel.IsSimulateButtonEnabled = false;
                return;
            }

            var validationResult = _mainViewModel.InputViewModel.Validate(_mainViewModel);

            if (validationResult.IsValid)
            {
                _mainViewModel.InputViewModel.ErrorMessage = string.Empty;
                _mainViewModel.InputViewModel.IsErrorVisible = false;
            }
            else
            {
                _mainViewModel.InputViewModel.ErrorMessage = validationResult.Errors[0].ErrorMessage;
                _mainViewModel.InputViewModel.IsErrorVisible = true;
            }

            _mainViewModel.IsValid = validationResult.IsValid;
            _mainViewModel.IsSimulateButtonEnabled = validationResult.IsValid;
        }
    }

    private readonly MainViewModel _mainViewModel;
}
