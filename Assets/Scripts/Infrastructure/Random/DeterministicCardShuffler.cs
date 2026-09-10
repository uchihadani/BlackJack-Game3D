using System;
using System.Collections.Generic;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Infrastructure.Random
{
    public sealed class DeterministicCardShuffler : ICardShuffler, IDeckEntryShuffler
    {
        public IReadOnlyList<NumericCard> Shuffle(
            IReadOnlyList<NumericCard> cards,
            int seed,
            int streamIndex)
        {
            return ShuffleCore(cards, seed, streamIndex, nameof(cards));
        }

        public IReadOnlyList<DeckEntry> ShuffleEntries(
            IReadOnlyList<DeckEntry> entries,
            int seed,
            int streamIndex)
        {
            return ShuffleCore(entries, seed, streamIndex, nameof(entries));
        }

        private static IReadOnlyList<T> ShuffleCore<T>(
            IReadOnlyList<T> source,
            int seed,
            int streamIndex,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (streamIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }

            T[] shuffledValues = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                shuffledValues[index] = source[index];
            }

            StableRandom random = new StableRandom(CreateState(seed, streamIndex));
            for (int index = shuffledValues.Length - 1; index > 0; index--)
            {
                int swapIndex = random.NextInt(index + 1);
                T temporary = shuffledValues[index];
                shuffledValues[index] = shuffledValues[swapIndex];
                shuffledValues[swapIndex] = temporary;
            }

            return shuffledValues;
        }

        private static uint CreateState(int seed, int streamIndex)
        {
            unchecked
            {
                uint state = (uint)seed;
                state += 0x9E3779B9u * ((uint)streamIndex + 1u);
                state ^= state >> 16;
                state *= 0x85EBCA6Bu;
                state ^= state >> 13;
                state *= 0xC2B2AE35u;
                state ^= state >> 16;
                return state == 0u ? 0xA341316Cu : state;
            }
        }

        private struct StableRandom
        {
            private uint _state;

            public StableRandom(uint state)
            {
                _state = state;
            }

            public int NextInt(int maximumExclusive)
            {
                if (maximumExclusive <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(maximumExclusive));
                }

                uint bound = (uint)maximumExclusive;
                uint threshold = unchecked(0u - bound) % bound;
                uint value;
                do
                {
                    value = NextUInt();
                }
                while (value < threshold);

                return (int)(value % bound);
            }

            private uint NextUInt()
            {
                uint value = _state;
                value ^= value << 13;
                value ^= value >> 17;
                value ^= value << 5;
                _state = value;
                return value;
            }
        }
    }
}
