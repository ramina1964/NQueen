namespace NQueen.GUI.Utils;

public static class AutomationPropertiesHelper
{
    public static readonly DependencyProperty IsOffscreenProperty =
        DependencyProperty.RegisterAttached(
            "IsOffscreen",
            typeof(bool),
            typeof(AutomationPropertiesHelper),
            new PropertyMetadata(false));

    public static bool GetIsOffscreen(UIElement element)
    {
        return (bool)element.GetValue(IsOffscreenProperty);
    }

    public static void SetIsOffscreen(UIElement element, bool value)
    {
        element.SetValue(IsOffscreenProperty, value);
    }
}
