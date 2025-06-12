namespace NQueen.ConsoleApp;

public class App(DispatchCommands dispatchCommands)
{
    public void Run(string[] args)
    {
        _dispatchCommands.InitCommands();

        switch (args.Length)
        {
            case > 0:
                _dispatchCommands.ProcessCommandsFromArgs(args);
                return;

            default:
                _dispatchCommands.ProcessCommandsInteractively();
                return;
        }
    }

    private readonly DispatchCommands _dispatchCommands = dispatchCommands
            ?? throw new ArgumentNullException(nameof(dispatchCommands));
}
