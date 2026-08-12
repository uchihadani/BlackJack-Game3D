using System;

namespace TwentyThree.Domain.Economy
{
    public readonly struct PayoutRules
    {
        public BasisPoints NormalWin { get; }

        public BasisPoints AllInWin { get; }

        public BasisPoints InitialTwentyThree { get; }

        public BasisPoints AllInInitialTwentyThree { get; }

        public PayoutRules(
            BasisPoints normalWin,
            BasisPoints allInWin,
            BasisPoints initialTwentyThree,
            BasisPoints allInInitialTwentyThree)
        {
            NormalWin = normalWin;
            AllInWin = allInWin;
            InitialTwentyThree = initialTwentyThree;
            AllInInitialTwentyThree = allInInitialTwentyThree;
        }

        public BasisPoints GetNetGain(BetOutcome outcome, bool isAllIn)
        {
            switch (outcome)
            {
                case BetOutcome.Loss:
                case BetOutcome.Draw:
                    return BasisPoints.Zero;
                case BetOutcome.NormalWin:
                    return isAllIn ? AllInWin : NormalWin;
                case BetOutcome.InitialTwentyThree:
                    return isAllIn ? AllInInitialTwentyThree : InitialTwentyThree;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }
    }
}
