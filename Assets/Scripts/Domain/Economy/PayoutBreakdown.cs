using System;

namespace TwentyThree.Domain.Economy
{
    public readonly struct PayoutBreakdown : IEquatable<PayoutBreakdown>
    {
        public PayoutBreakdown(
            Money returnedBet,
            Money baseNetGain,
            Money bonusNetGain)
        {
            ReturnedBet = returnedBet;
            BaseNetGain = baseNetGain;
            BonusNetGain = bonusNetGain;
            Total = returnedBet + baseNetGain + bonusNetGain;
        }

        public Money ReturnedBet { get; }

        public Money BaseNetGain { get; }

        public Money BonusNetGain { get; }

        public Money Total { get; }

        public bool Equals(PayoutBreakdown other)
        {
            return ReturnedBet == other.ReturnedBet &&
                   BaseNetGain == other.BaseNetGain &&
                   BonusNetGain == other.BonusNetGain;
        }

        public override bool Equals(object obj)
        {
            return obj is PayoutBreakdown other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ReturnedBet, BaseNetGain, BonusNetGain);
        }
    }
}
