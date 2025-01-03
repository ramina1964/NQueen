﻿namespace NQueen.ConsoleApp;

public class App
{
    public App(ISolver solver, IConsoleUtils consoleUtils, DispatchCommands dispatchCommands)
    {
        _solver = solver
            ?? throw new ArgumentNullException(nameof(solver));

        _consoleUtils = consoleUtils
            ?? throw new ArgumentNullException(nameof(consoleUtils));

        _dispatchCommands = dispatchCommands
            ?? throw new ArgumentNullException(nameof(dispatchCommands));
    }

    public void Run(string[] args)
    {
        _dispatchCommands.InitCommands();

        if (args.Length > 0)
        {
            _dispatchCommands.ProcessCommandsFromArgs(args);
        }
        else
        {
            _dispatchCommands.ProcessCommandsInteractively();
        }
    }

    private readonly ISolver _solver;
    private readonly IConsoleUtils _consoleUtils;
    private readonly DispatchCommands _dispatchCommands;
}
