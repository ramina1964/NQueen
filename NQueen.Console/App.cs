namespace NQueen.ConsoleApp;

public class App
{
    public App(ISolver solver)
    {
        _solver = solver;
    }

    public void Run()
    {

    }

    private readonly ISolver _solver;
}
