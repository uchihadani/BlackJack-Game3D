namespace TwentyThree.Application.Navigation
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        bool TryLoad(string sceneName);
    }
}
