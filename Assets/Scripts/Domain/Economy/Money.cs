using System;

namespace TwentyThree.Domain.Economy
{
    public readonly struct Money : IEquatable<Money>, IComparable<Money>
    {
        public const long MinorUnitsPerCoin = 100;

        public static Money Zero => new Money(0);

        public long MinorUnits { get; }

        private Money(long minorUnits)
        {
            MinorUnits = minorUnits;
        }

        public static Money FromMinorUnits(long minorUnits)
        {
            if (minorUnits < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minorUnits));
            }

            return new Money(minorUnits);
        }

        public static Money FromCoins(long coins)
        {
            if (coins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(coins));
            }

            return new Money(checked(coins * MinorUnitsPerCoin));
        }

        public Money ApplyPercentage(BasisPoints percentage)
        {
            if (MinorUnits == 0 || percentage.Value == 0)
            {
                return Zero;
            }

            long wholeUnits = MinorUnits / BasisPoints.Scale;
            long remainderUnits = MinorUnits % BasisPoints.Scale;
            long wholeResult = checked(wholeUnits * percentage.Value);
            long fractionalNumerator = remainderUnits * percentage.Value;
            long fractionalResult = fractionalNumerator / BasisPoints.Scale;

            if (fractionalNumerator % BasisPoints.Scale >= BasisPoints.Scale / 2)
            {
                fractionalResult = checked(fractionalResult + 1);
            }

            return new Money(checked(wholeResult + fractionalResult));
        }

        public int CompareTo(Money other)
        {
            return MinorUnits.CompareTo(other.MinorUnits);
        }

        public bool Equals(Money other)
        {
            return MinorUnits == other.MinorUnits;
        }

        public override bool Equals(object obj)
        {
            return obj is Money other && Equals(other);
        }

        public override int GetHashCode()
        {
            return MinorUnits.GetHashCode();
        }

        public override string ToString()
        {
            return MinorUnits.ToString();
        }

        public static Money operator +(Money left, Money right)
        {
            return new Money(checked(left.MinorUnits + right.MinorUnits));
        }

        public static Money operator -(Money left, Money right)
        {
            if (right > left)
            {
                throw new InvalidOperationException("Money cannot be negative.");
            }

            return new Money(left.MinorUnits - right.MinorUnits);
        }

        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(Money left, Money right)
        {
            return left.MinorUnits < right.MinorUnits;
        }

        public static bool operator <=(Money left, Money right)
        {
            return left.MinorUnits <= right.MinorUnits;
        }

        public static bool operator >(Money left, Money right)
        {
            return left.MinorUnits > right.MinorUnits;
        }

        public static bool operator >=(Money left, Money right)
        {
            return left.MinorUnits >= right.MinorUnits;
        }
    }
}
