using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class PayoutCalculator
    {
        public Money Calculate(LockedBet bet, BetOutcome outcome, PayoutRules rules)
        {
            if (bet == null)
            {
                throw new ArgumentNullException(nameof(bet));
            }

            switch (outcome)
            {
                case BetOutcome.Loss:
                    return Money.Zero;
                case BetOutcome.Draw:
                    return bet.Amount;
                case BetOutcome.NormalWin:
                case BetOutcome.InitialTwentyThree:
                    BasisPoints netGain = rules.GetNetGain(outcome, bet.IsAllIn);
                    return bet.Amount + bet.Amount.ApplyPercentage(netGain);
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }

        public bool TrySettle(
            LockedBet bet,
            BetOutcome outcome,
            PayoutRules rules,
            Wallet wallet,
            out Money payout)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            Money calculated = Calculate(bet, outcome, rules);

            if (!bet.TrySettle(wallet, calculated))
            {
                payout = Money.Zero;
                return false;
            }

            payout = calculated;
            return true;
        }
    }
}
