namespace NQueen.GUI.Views;

// Todo: Fix these errors which appear when running Accessibility Checker:
//Severity Automation ID Rule    Description How To Fix  File Line    Project
//1) Error       NameNotNull The Name property of a focusable element must not be null.	Provide a UI Automation Name property that concisely identifies the element.D:\Repos\Code\NQueen\NQueen.GUI\Views\ChessboardUserControl.xaml	16	NQueen.GUI
//2) Error       BoundingRectangleNotNull An on-screen element must not have a null BoundingRectangle property.If the element is off-screen, set its IsOffscreen property to true. If the element is on-screen, provide a BoundingRectangle property.	D:\Repos\Code\NQueen\NQueen.GUI\Views\ChessboardUserControl.xaml    36	NQueen.GUI

public partial class ChessboardUserControl : UserControl
{
    public ChessboardUserControl()
    {
        InitializeComponent();
    }
}
