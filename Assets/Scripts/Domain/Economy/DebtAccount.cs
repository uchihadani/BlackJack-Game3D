using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class DebtAccount
    {
        private readonly object sync = new object();
        private Money remaining;

        public DebtAccount(Money initialDebt)
        {
            remaining = initialDebt;
        }

        public Money Remaining
        {
            get
            {
                lock (sync)
                {
                    return remaining;
                }
            }
        }

        public bool IsPaid
        {
            get
            {
                lock (sync)
                {
                    return remaining == Money.Zero;
                }
            }
        }

        public bool TryPayFromAvailable(Wallet wallet, Money amount)
        {
            return TryPay(wallet, amount, false);
        }

        public bool TryPayFromProtected(Wallet wallet, Money amount)
        {
            return TryPay(wallet, amount, true);
        }

        public Money ApplyInterest(BasisPoints interestRate)
        {
            lock (sync)
            {
                if (remaining == Money.Zero)
                {
                    return Money.Zero;
                }

                Money interest = remaining.ApplyPercentage(interestRate);
                Money updated = remaining + interest;
                remaining = updated;
                return interest;
            }
        }

        private bool TryPay(Wallet wallet, Money amount, bool useProtectedFunds)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (amount == Money.Zero)
            {
                return false;
            }

            lock (sync)
            {
                if (amount > remaining)
                {
                    return false;
                }

                bool debited = useProtectedFunds
                    ? wallet.TryDebitProtected(amount)
                    : wallet.TryDebitAvailable(amount);

                if (!debited)
                {
                    return false;
                }

                remaining -= amount;
                return true;
            }
        }
    }
}
