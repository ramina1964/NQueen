namespace NQueen.ConsoleApp;

public class App(ISolver solver)
{
    public void Run()
    {

    }

    private readonly ISolver _solver = solver;
}
