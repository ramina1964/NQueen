namespace NQueen.ConsoleApp.Commands;

public class CommandProcessor() : ICommandProcessor
{

    // The new menu-driven DispatchCommands does not support instance-based commands.
    // Inform the user to use the menu-driven interface instead.
    public bool ProcessCommand(string key, string value, DispatchCommands dispatch)
    {
        Console.WriteLine("This command interface is deprecated. " +
            "Please use the menu-driven interface by running the application without" +
            "command-line arguments.");

        return false;
    }

    public void ProcessCommandsFromArgs(string[] args, DispatchCommands dispatch) =>
        Console.WriteLine("Command-line argument processing is deprecated. Please use the menu-driven interface by running the application without command-line arguments.");

    public void ProcessCommandsInteractively(DispatchCommands dispatch) =>
        Console.WriteLine("Interactive command processing is deprecated. Please use the menu-driven interface by running the application without command-line arguments.");
}