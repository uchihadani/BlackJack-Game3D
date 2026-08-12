using System;

namespace TwentyThree.Domain.Economy
{
    public readonly struct BetRules
    {
        public Money Minimum { get; }

        public Money Maximum { get; }

        public BetRules(Money minimum, Money maximum)
        {
            if (minimum == Money.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum));
            }

            if (maximum < minimum)
            {
                throw new ArgumentException("Maximum must be greater than or equal to minimum.", nameof(maximum));
            }

            Minimum = minimum;
            Maximum = maximum;
        }

        public bool AllowsStandardBet(Money amount)
        {
            return Minimum != Money.Zero && amount >= Minimum && amount <= Maximum;
        }

        public bool AllowsAllIn(Money available)
        {
            return Minimum != Money.Zero && available >= Minimum;
        }
    }
}
