namespace TwentyThree.Application.Navigation
{
    public interface IGameFlow
    {
        bool IsTransitioning { get; }

        bool ShowMainMenu();

        bool StartNewRun();

        void Quit();
    }
}
