using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class Wallet
    {
        private readonly object sync = new object();
        private Money available;
        private Money protectedFunds;

        internal object SynchronizationRoot => sync;

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

        internal bool TryDebitAllocated(Money fromAvailable, Money fromProtected)
        {
            lock (sync)
            {
                if (fromAvailable > available || fromProtected > protectedFunds)
                {
                    return false;
                }

                Money updatedAvailable = available - fromAvailable;
                Money updatedProtected = protectedFunds - fromProtected;
                available = updatedAvailable;
                protectedFunds = updatedProtected;
                return true;
            }
        }

        internal bool TryTransferAvailableToProtected(Money amount, Money capacity)
        {
            lock (sync)
            {
                if (amount == Money.Zero || amount > available || protectedFunds > capacity)
                {
                    return false;
                }

                Money remainingCapacity = capacity - protectedFunds;

                if (amount > remainingCapacity)
                {
                    return false;
                }

                Money updatedAvailable = available - amount;
                Money updatedProtected = protectedFunds + amount;
                available = updatedAvailable;
                protectedFunds = updatedProtected;
                return true;
            }
        }

        internal bool TryTransferProtectedToAvailable(Money amount)
        {
            lock (sync)
            {
                if (amount == Money.Zero || amount > protectedFunds)
                {
                    return false;
                }

                Money updatedAvailable;

                try
                {
                    updatedAvailable = available + amount;
                }
                catch (OverflowException)
                {
                    return false;
                }

                Money updatedProtected = protectedFunds - amount;
                available = updatedAvailable;
                protectedFunds = updatedProtected;
                return true;
            }
        }
    }
}
