using System;

namespace TwentyThree.Domain.Randomness
{
    public readonly struct RandomStreamState : IEquatable<RandomStreamState>
    {
        public RandomStreamState(int seed, RandomStreamKey key, long position)
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
            Position = position;
        }

        public int Seed { get; }

        public RandomStreamKey Key { get; }

        public long Position { get; }

        public bool Equals(RandomStreamState other)
        {
            return Seed == other.Seed &&
                   Key == other.Key &&
                   Position == other.Position;
        }

        public override bool Equals(object obj)
        {
            return obj is RandomStreamState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)Seed) * 16777619u;

                string keyValue = Key.Value ?? string.Empty;
                for (int index = 0; index < keyValue.Length; index++)
                {
                    char character = keyValue[index];
                    hash = (hash ^ (byte)character) * 16777619u;
                    hash = (hash ^ (byte)(character >> 8)) * 16777619u;
                }

                hash = (hash ^ (uint)Position) * 16777619u;
                hash = (hash ^ (uint)((ulong)Position >> 32)) * 16777619u;
                return (int)hash;
            }
        }

        public static bool operator ==(RandomStreamState left, RandomStreamState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RandomStreamState left, RandomStreamState right)
        {
            return !left.Equals(right);
        }
    }
}
