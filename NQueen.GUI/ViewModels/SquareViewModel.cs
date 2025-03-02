namespace NQueen.GUI.ViewModels;

public partial class SquareViewModel : ObservableObject
{
    public SquareViewModel(Position pos, Brush color)
    {
        Position = pos;
        Color = color;
    }

    [ObservableProperty]
    private Brush _color;

    [ObservableProperty]
    private Position _position;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private string _imagePath;

    [ObservableProperty]
    private bool _isOffscreen;

    public Rect BoundingRectangle => new(0, 0, Width, Height);

    public override string ToString()
    {
        return $"{Position.RowNo}, {Position.ColumnNo}";
    }
}

