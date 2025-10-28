namespace NQueen.Domain.Enums;

public enum ResultStorageMode
{
    [Description("Sample (capped)")]
    Materialize = 0,
    [Description("Count Only")]
    CountOnly = 1
}
