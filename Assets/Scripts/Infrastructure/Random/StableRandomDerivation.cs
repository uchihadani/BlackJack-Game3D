using TwentyThree.Domain.Randomness;

namespace TwentyThree.Infrastructure.Random
{
    internal static class StableRandomDerivation
    {
        private const ulong OffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;
        private const ulong PositionStep = 0x9E3779B97F4A7C15UL;
        private const ulong AttemptStep = 0xD1B54A32D192ED03UL;

        public static ulong CreateSequenceSeed(int seed, RandomStreamKey key)
        {
            unchecked
            {
                ulong hash = OffsetBasis;
                uint seedBits = (uint)seed;
                hash = AddByte(hash, (byte)seedBits);
                hash = AddByte(hash, (byte)(seedBits >> 8));
                hash = AddByte(hash, (byte)(seedBits >> 16));
                hash = AddByte(hash, (byte)(seedBits >> 24));

                string keyValue = key.Value;
                for (int index = 0; index < keyValue.Length; index++)
                {
                    char character = keyValue[index];
                    hash = AddByte(hash, (byte)character);
                    hash = AddByte(hash, (byte)(character >> 8));
                }

                return Mix(hash);
            }
        }

        public static ulong ValueAt(ulong sequenceSeed, long position, ulong attempt)
        {
            unchecked
            {
                ulong input = sequenceSeed;
                input += PositionStep * ((ulong)position + 1UL);
                input += AttemptStep * attempt;
                return Mix(input);
            }
        }

        private static ulong AddByte(ulong hash, byte value)
        {
            unchecked
            {
                return (hash ^ value) * FnvPrime;
            }
        }

        private static ulong Mix(ulong value)
        {
            unchecked
            {
                value ^= value >> 30;
                value *= 0xBF58476D1CE4E5B9UL;
                value ^= value >> 27;
                value *= 0x94D049BB133111EBUL;
                value ^= value >> 31;
                return value;
            }
        }
    }
}
