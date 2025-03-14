namespace NQueen.GUI.ViewModels;

public interface IEventManager
{
    void SubscribeToSimulationEvents();

    void UnsubscribeFromSimulationEvents();

    void OnBoardSizeChanged();

    void OnSolutionModeChanged(SolutionMode value);

    void OnDisplayModeChanged();
}
