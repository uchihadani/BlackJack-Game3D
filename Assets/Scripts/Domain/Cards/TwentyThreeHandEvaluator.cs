using System;

namespace TwentyThree.Domain.Cards
{
    public sealed class TwentyThreeHandEvaluator : IHandEvaluator
    {
        public const int TargetTotal = 23;

        public HandScore Evaluate(Hand hand)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand));
            }

            int total = 0;
            int softAceCount = 0;

            foreach (NumericCard card in hand.Cards)
            {
                total += hand.GetMechanicalValue(card);
                if (hand.IsFlexibleAce(card))
                {
                    softAceCount++;
                }
            }

            while (total > TargetTotal && softAceCount > 0)
            {
                total -= 10;
                softAceCount--;
            }

            int adjustedTotal = checked(total + hand.TotalModifier);
            if (adjustedTotal < 0)
            {
                throw new InvalidOperationException("A hand total modifier produced a negative total.");
            }

            return new HandScore(
                adjustedTotal,
                hand.Count,
                softAceCount,
                hand.TotalModifier != 0);
        }
    }
}
