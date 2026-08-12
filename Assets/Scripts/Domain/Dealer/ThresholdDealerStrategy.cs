using System;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Domain.Dealer
{
    public sealed class ThresholdDealerStrategy : IDealerStrategy
    {
        public ThresholdDealerStrategy(int standThreshold)
        {
            if (standThreshold < 1 || standThreshold > TwentyThreeHandEvaluator.TargetTotal)
            {
                throw new ArgumentOutOfRangeException(nameof(standThreshold));
            }

            StandThreshold = standThreshold;
        }

        public int StandThreshold { get; }

        public DealerDecision Decide(HandScore score)
        {
            if (score.IsBust)
            {
                return DealerDecision.Bust;
            }

            return score.Total < StandThreshold
                ? DealerDecision.Hit
                : DealerDecision.Stand;
        }
    }
}
