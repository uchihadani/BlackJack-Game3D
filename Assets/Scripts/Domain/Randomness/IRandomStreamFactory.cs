namespace TwentyThree.Domain.Randomness
{
    public interface IRandomStreamFactory
    {
        IRandomStream Create(int seed, RandomStreamKey key);

        IRandomStream Restore(RandomStreamState state);
    }
}
