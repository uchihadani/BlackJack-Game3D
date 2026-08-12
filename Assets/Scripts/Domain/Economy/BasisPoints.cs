using System;

namespace TwentyThree.Domain.Economy
{
    public readonly struct BasisPoints : IEquatable<BasisPoints>, IComparable<BasisPoints>
    {
        public const int Scale = 10000;

        public static BasisPoints Zero => new BasisPoints(0);

        public int Value { get; }

        public BasisPoints(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public int CompareTo(BasisPoints other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(BasisPoints other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BasisPoints other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(BasisPoints left, BasisPoints right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BasisPoints left, BasisPoints right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(BasisPoints left, BasisPoints right)
        {
            return left.Value < right.Value;
        }

        public static bool operator <=(BasisPoints left, BasisPoints right)
        {
            return left.Value <= right.Value;
        }

        public static bool operator >(BasisPoints left, BasisPoints right)
        {
            return left.Value > right.Value;
        }

        public static bool operator >=(BasisPoints left, BasisPoints right)
        {
            return left.Value >= right.Value;
        }
    }
}
