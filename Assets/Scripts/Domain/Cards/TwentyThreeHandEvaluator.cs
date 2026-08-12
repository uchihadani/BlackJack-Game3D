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
                total += card.Value;
                if (card.IsAce)
                {
                    softAceCount++;
                }
            }

            while (total > TargetTotal && softAceCount > 0)
            {
                total -= 10;
                softAceCount--;
            }

            return new HandScore(total, hand.Count, softAceCount);
        }
    }
}
