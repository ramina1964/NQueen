namespace NQueen.ConsoleApp;

public class App(DispatchCommands dispatchCommands, IServiceProvider serviceProvider)
{
    public Task Run(string[] args)
    {
        // Use the new menu-driven entry point
        DispatchCommands.RunInteractiveMenu(serviceProvider);
        return Task.CompletedTask;
    }

    private readonly DispatchCommands _dispatchCommands = dispatchCommands
        ?? throw new ArgumentNullException(nameof(dispatchCommands));
}
