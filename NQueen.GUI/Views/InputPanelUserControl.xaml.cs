namespace NQueen.GUI.Views;

public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl(MainViewModel mainViewModel)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        DataContext = _mainViewModel;
    }

    private readonly MainViewModel _mainViewModel;

    private void TxtBoardSize_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // Validate the input string format
            if (!InputValidator.IsBoardSizeFormattedCorrectly(textBox.Text))
            {
                _mainViewModel.InputViewModel.ErrorMessage = Messages.InvalidByteError;
                _mainViewModel.InputViewModel.IsErrorVisible = true;
                _mainViewModel.IsInputValid = false;
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

            // Update the state of the "Simulate" button
            _mainViewModel.IsInputValid = validationResult.IsValid;
        }
    }
}
