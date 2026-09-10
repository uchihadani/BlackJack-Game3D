using System;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Infrastructure.Random
{
    internal sealed class DeterministicRandomStream : IRandomStream
    {
        private readonly ulong _sequenceSeed;
        private long _position;

        public DeterministicRandomStream(int seed, RandomStreamKey key, long position)
        {
            if (!key.IsDefined)
            {
                throw new ArgumentException("A random stream key is required.", nameof(key));
            }

            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            Seed = seed;
            Key = key;
            _position = position;
            _sequenceSeed = StableRandomDerivation.CreateSequenceSeed(seed, key);
        }

        public int Seed { get; }

        public RandomStreamKey Key { get; }

        public long Position => _position;

        public int NextInt(int maximumExclusive)
        {
            if (maximumExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
            }

            if (_position == long.MaxValue)
            {
                throw new InvalidOperationException("The random stream position is exhausted.");
            }

            ulong bound = (uint)maximumExclusive;
            ulong threshold = unchecked(0UL - bound) % bound;
            ulong attempt = 0;
            ulong candidate;

            do
            {
                candidate = StableRandomDerivation.ValueAt(
                    _sequenceSeed,
                    _position,
                    attempt);
                attempt = checked(attempt + 1UL);
            }
            while (candidate < threshold);

            _position++;
            return (int)(candidate % bound);
        }

        public RandomStreamState CaptureState()
        {
            return new RandomStreamState(Seed, Key, _position);
        }
    }
}
