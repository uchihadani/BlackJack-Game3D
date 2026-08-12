using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class Wallet
    {
        private readonly object sync = new object();
        private Money available;
        private Money protectedFunds;

        public Wallet(Money available)
            : this(available, Money.Zero)
        {
        }

        public Wallet(Money available, Money protectedFunds)
        {
            this.available = available;
            this.protectedFunds = protectedFunds;
        }

        public Money Available
        {
            get
            {
                lock (sync)
                {
                    return available;
                }
            }
        }

        public Money Protected
        {
            get
            {
                lock (sync)
                {
                    return protectedFunds;
                }
            }
        }

        public bool TryDebitAvailable(Money amount)
        {
            lock (sync)
            {
                if (amount > available)
                {
                    return false;
                }

                available -= amount;
                return true;
            }
        }

        public bool TryDebitProtected(Money amount)
        {
            lock (sync)
            {
                if (amount > protectedFunds)
                {
                    return false;
                }

                protectedFunds -= amount;
                return true;
            }
        }

        public void CreditAvailable(Money amount)
        {
            lock (sync)
            {
                Money updated = available + amount;
                available = updated;
            }
        }

        public void CreditProtected(Money amount)
        {
            lock (sync)
            {
                Money updated = protectedFunds + amount;
                protectedFunds = updated;
            }
        }
    }
}
