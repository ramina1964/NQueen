namespace NQueen.ConsoleApp;

public class App(IServiceProvider serviceProvider)
{
    public Task Run(string[] args)
    {
        DispatchCommands.RunInteractiveMenu(serviceProvider);
        return Task.CompletedTask;
    }
}
