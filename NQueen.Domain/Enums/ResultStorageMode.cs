namespace NQueen.Domain.Enums;

public enum ResultStorageMode
{
    [Description("Sample (capped)")]
    MaterializeSample = 0,
    [Description("Count Only")]
    CountOnly = 1
}
