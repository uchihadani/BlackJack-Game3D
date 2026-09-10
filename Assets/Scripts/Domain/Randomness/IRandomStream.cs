namespace TwentyThree.Domain.Randomness
{
    public interface IRandomStream
    {
        int Seed { get; }

        RandomStreamKey Key { get; }

        long Position { get; }

        int NextInt(int maximumExclusive);

        RandomStreamState CaptureState();
    }
}
