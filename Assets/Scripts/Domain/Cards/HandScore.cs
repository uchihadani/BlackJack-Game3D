using System;

namespace TwentyThree.Domain.Cards
{
    public readonly struct HandScore : IEquatable<HandScore>
    {
        public HandScore(int total, int cardCount, int softAceCount)
        {
            if (total < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(total));
            }

            if (cardCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardCount));
            }

            if (softAceCount < 0 || softAceCount > cardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(softAceCount));
            }

            Total = total;
            CardCount = cardCount;
            SoftAceCount = softAceCount;
        }

        public int Total { get; }

        public int CardCount { get; }

        public int SoftAceCount { get; }

        public bool IsBust => Total > TwentyThreeHandEvaluator.TargetTotal;

        public bool IsTwentyThree => Total == TwentyThreeHandEvaluator.TargetTotal;

        public bool IsInitialTwentyThree => IsTwentyThree && CardCount == 3;

        public bool Equals(HandScore other)
        {
            return Total == other.Total &&
                   CardCount == other.CardCount &&
                   SoftAceCount == other.SoftAceCount;
        }

        public override bool Equals(object obj)
        {
            return obj is HandScore other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Total, CardCount, SoftAceCount);
        }
    }
}
