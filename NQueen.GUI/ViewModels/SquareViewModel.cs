namespace NQueen.GUI.ViewModels;

public class SquareViewModel : ObservableObject
{
    #region Constructor
    public SquareViewModel(Position pos, Brush color)
    {
        Color = color;
        Position = pos;
    }
    #endregion Constructor

    #region PublicProperties

    public Brush Color { get; set; }

    public Position Position { get; set; }

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
    #endregion PublicProperties

    #region PrivateFields
    private double _width;
    private double _height;
    private string _imagePath;
    #endregion PrivateFields
}
