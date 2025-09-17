namespace NQueen.ConsoleApp.Interfaces;

public interface ICommandProcessor
{
    Task<bool> ProcessCommand(string key, string value, DispatchCommands dispatchCommands);

    Task ProcessCommandsFromArgs(string[] args, DispatchCommands dispatchCommands);

    Task ProcessCommandsInteractively(DispatchCommands dispatchCommands);
}
