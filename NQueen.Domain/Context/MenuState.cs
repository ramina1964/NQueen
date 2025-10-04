namespace NQueen.Domain.Context;

public record MenuState
{
    public bool ExitRequested { get; set; }
 
    public int BlankInputCount { get; set; }
}