using System;

namespace TwentyThree.Domain.Cards
{
    public readonly struct CardId : IEquatable<CardId>, IComparable<CardId>
    {
        public const int MinimumValue = 0;
        public const int MaximumValue = 51;

        public CardId(int value)
        {
            if (value < MinimumValue || value > MaximumValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int Value { get; }

        public int CompareTo(CardId other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(CardId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CardId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString("D2");
        }

        public static bool operator ==(CardId left, CardId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardId left, CardId right)
        {
            return !left.Equals(right);
        }
    }
}
