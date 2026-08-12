using System;

namespace TwentyThree.Domain.Cards
{
    public readonly struct DeckReshuffleRecord : IEquatable<DeckReshuffleRecord>
    {
        public DeckReshuffleRecord(int roundSeed, int streamIndex, int recycledCardCount)
        {
            if (streamIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }

            if (recycledCardCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(recycledCardCount));
            }

            RoundSeed = roundSeed;
            StreamIndex = streamIndex;
            RecycledCardCount = recycledCardCount;
        }

        public int RoundSeed { get; }

        public int StreamIndex { get; }

        public int RecycledCardCount { get; }

        public bool Equals(DeckReshuffleRecord other)
        {
            return RoundSeed == other.RoundSeed &&
                   StreamIndex == other.StreamIndex &&
                   RecycledCardCount == other.RecycledCardCount;
        }

        public override bool Equals(object obj)
        {
            return obj is DeckReshuffleRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoundSeed, StreamIndex, RecycledCardCount);
        }
    }
}
