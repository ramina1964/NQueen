namespace NQueen.GUI.ViewModels;

public class SquareViewModel(Position pos, Brush color) : ObservableObject
{
    public Brush Color { get; } = color;

    public Position Position { get; } = pos;

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public string ImagePath
    {
        get => _imagePath;
        set => SetProperty(ref _imagePath, value);
    }

    public override string ToString() => $"{Position.RowNo}, {Position.ColumnNo}";

    private double _width;
    private double _height;
    private string _imagePath = string.Empty;
}
