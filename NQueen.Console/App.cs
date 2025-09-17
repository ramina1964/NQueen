namespace NQueen.ConsoleApp;

public class App(DispatchCommands dispatchCommands)
{
    public async Task Run(string[] args)
    {
        _dispatchCommands.InitCommands();

        switch (args.Length)
        {
            case > 0:
                await _dispatchCommands.ProcessCommandsFromArgs(args);
                return;

            default:
                await _dispatchCommands.ProcessCommandsInteractively();
                return;
        }
    }

    private readonly DispatchCommands _dispatchCommands = dispatchCommands
        ?? throw new ArgumentNullException(nameof(dispatchCommands));
}
