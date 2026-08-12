namespace TwentyThree.Application.Gameplay
{
    public interface IGameSessionFactory
    {
        IGameSession Create(int seed);
    }
}
