namespace NQueen.ConsoleApp;

public class App(DispatchCommands dispatchCommands)
{
    public void Run(string[] args)
    {
        dispatchCommands.InitCommands();

        if (args.Length > 0)
        {
            dispatchCommands.ProcessCommandsFromArgs(args);
        }
        else
        {
            dispatchCommands.ProcessCommandsInteractively();
        }
    }
}
