namespace TwentyThree.Domain.Economy
{
    public sealed class LockedBet
    {
        private readonly object sync = new object();
        private bool isSettled;

        internal LockedBet(Money amount, bool isAllIn)
        {
            Amount = amount;
            IsAllIn = isAllIn;
        }

        public Money Amount { get; }

        public bool IsAllIn { get; }

        public bool IsSettled
        {
            get
            {
                lock (sync)
                {
                    return isSettled;
                }
            }
        }

        internal bool TrySettle(Wallet wallet, Money payout)
        {
            lock (sync)
            {
                if (isSettled)
                {
                    return false;
                }

                wallet.CreditAvailable(payout);
                isSettled = true;
                return true;
            }
        }
    }
}
