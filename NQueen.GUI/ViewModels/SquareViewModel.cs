namespace NQueen.GUI.ViewModels;

public partial class SquareViewModel : ObservableObject
{
    public SquareViewModel(Position pos, Brush color)
    {
        Position = pos;
        Color = color;
        UpdateBoundingRectangle();
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

    public Rect BoundingRectangle { get; private set; }

    partial void OnWidthChanged(double value)
    {
        UpdateBoundingRectangle();
    }

    partial void OnHeightChanged(double value)
    {
        UpdateBoundingRectangle();
    }

    private void UpdateBoundingRectangle()
    {
        if (Width > 0 && Height > 0)
        {
            BoundingRectangle = new Rect(0, 0, Width, Height);
        }
        else
        {
            BoundingRectangle = Rect.Empty;
        }
        OnPropertyChanged(nameof(BoundingRectangle));
    }

    public override string ToString()
    {
        return $"{Position.RowNo}, {Position.ColumnNo}";
    }
}
