using System;
using System.Globalization;

namespace TwentyThree.Domain.Randomness
{
    public readonly struct RandomStreamKey : IEquatable<RandomStreamKey>
    {
        public RandomStreamKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A random stream key is required.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrEmpty(Value);

        public RandomStreamKey Derive(int component)
        {
            if (!IsDefined)
            {
                throw new InvalidOperationException("The random stream key is not defined.");
            }

            if (component < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(component));
            }

            return new RandomStreamKey(string.Concat(
                Value,
                "/",
                component.ToString(CultureInfo.InvariantCulture)));
        }

        public bool Equals(RandomStreamKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RandomStreamKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                uint hash = 2166136261u;
                string value = Value ?? string.Empty;
                for (int index = 0; index < value.Length; index++)
                {
                    char character = value[index];
                    hash = (hash ^ (byte)character) * 16777619u;
                    hash = (hash ^ (byte)(character >> 8)) * 16777619u;
                }

                return (int)hash;
            }
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(RandomStreamKey left, RandomStreamKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RandomStreamKey left, RandomStreamKey right)
        {
            return !left.Equals(right);
        }
    }
}
