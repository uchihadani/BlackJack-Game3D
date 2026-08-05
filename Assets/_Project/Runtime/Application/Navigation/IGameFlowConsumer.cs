namespace TwentyThree.Application.Navigation
{
    /// <summary>
    /// Implemented by scene presenters that receive navigation from the composition root.
    /// This avoids static service locators and direct SceneManager calls from UI code.
    /// </summary>
    public interface IGameFlowConsumer
    {
        void Configure(IGameFlow gameFlow);
    }
}
