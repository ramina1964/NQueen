namespace NQueen.GUI.Views;

public partial class InputPanelUserControl : UserControl
{
    public InputPanelUserControl()
    {
        InitializeComponent();

        // Ensure the delay slider respects the domain minimum (5 ms)
        Loaded += (_, __) =>
        {
            if (DelaySlider != null)
            {
                DelaySlider.Minimum = SimulationSettings.MinDelayInMilliseconds;
                DelaySlider.TickFrequency = 1;
                DelaySlider.IsSnapToTickEnabled = true;
            }
        };
    }
}
