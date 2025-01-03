﻿namespace NQueen.GUI.ViewModels;

public class SquareViewModel : ObservableObject
{
    public SquareViewModel(Position pos, Brush color)
    {
        Position = pos;
        Color = color;
    }

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

    public bool IsOffscreen
    {
        get => _isOffscreen;
        set => SetProperty(ref _isOffscreen, value);
    }

    public override string ToString() => $"{Position.RowNo}, {Position.ColumnNo}";

    private double _width;
    private double _height;
    private string _imagePath;
    private bool _isOffscreen;
}
