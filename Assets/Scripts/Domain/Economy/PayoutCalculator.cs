using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class PayoutCalculator
    {
        public Money Calculate(LockedBet bet, BetOutcome outcome, PayoutRules rules)
        {
            return CalculateBreakdown(bet, outcome, rules, BasisPoints.Zero).Total;
        }

        public PayoutBreakdown CalculateBreakdown(
            LockedBet bet,
            BetOutcome outcome,
            PayoutRules rules,
            BasisPoints netGainBonus)
        {
            if (bet == null)
            {
                throw new ArgumentNullException(nameof(bet));
            }

            switch (outcome)
            {
                case BetOutcome.Loss:
                    return new PayoutBreakdown(Money.Zero, Money.Zero, Money.Zero);
                case BetOutcome.Draw:
                case BetOutcome.ProtectedDraw:
                    return new PayoutBreakdown(bet.Amount, Money.Zero, Money.Zero);
                case BetOutcome.NormalWin:
                case BetOutcome.InitialTwentyThree:
                    BasisPoints netGain = rules.GetNetGain(outcome, bet.IsAllIn);
                    Money baseNetGain = bet.Amount.ApplyPercentage(netGain);
                    Money bonus = baseNetGain.ApplyPercentage(netGainBonus);
                    return new PayoutBreakdown(bet.Amount, baseNetGain, bonus);
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
            bool settled = TrySettle(
                bet,
                outcome,
                rules,
                BasisPoints.Zero,
                wallet,
                out PayoutBreakdown breakdown);
            payout = settled ? breakdown.Total : Money.Zero;
            return settled;
        }

        public bool TrySettle(
            LockedBet bet,
            BetOutcome outcome,
            PayoutRules rules,
            BasisPoints netGainBonus,
            Wallet wallet,
            out PayoutBreakdown payout)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            PayoutBreakdown calculated = CalculateBreakdown(
                bet,
                outcome,
                rules,
                netGainBonus);

            if (!bet.TrySettle(wallet, calculated.Total))
            {
                payout = default;
                return false;
            }

            payout = calculated;
            return true;
        }
    }
}
