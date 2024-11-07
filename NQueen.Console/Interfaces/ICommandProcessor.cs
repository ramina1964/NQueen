namespace NQueen.ConsoleApp.Interfaces;

public interface ICommandProcessor
{
    bool ProcessCommand(string key, string value, DispatchCommands dispatchCommands);

    void ProcessCommandsFromArgs(string[] args, DispatchCommands dispatchCommands);
}