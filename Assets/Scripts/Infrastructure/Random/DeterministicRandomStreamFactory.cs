using TwentyThree.Domain.Randomness;

namespace TwentyThree.Infrastructure.Random
{
    public sealed class DeterministicRandomStreamFactory : IRandomStreamFactory
    {
        public IRandomStream Create(int seed, RandomStreamKey key)
        {
            return new DeterministicRandomStream(seed, key, 0);
        }

        public IRandomStream Restore(RandomStreamState state)
        {
            return new DeterministicRandomStream(
                state.Seed,
                state.Key,
                state.Position);
        }
    }
}
