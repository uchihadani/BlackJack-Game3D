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
            return TryPay(
                wallet,
                new DebtPaymentAllocation(amount, Money.Zero)).Succeeded;
        }

        public bool TryPayFromProtected(Wallet wallet, Money amount)
        {
            return TryPay(
                wallet,
                new DebtPaymentAllocation(Money.Zero, amount)).Succeeded;
        }

        public DebtPaymentResult TryPay(Wallet wallet, DebtPaymentAllocation allocation)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            lock (sync)
            {
                lock (wallet.SynchronizationRoot)
                {
                    if (!allocation.TryGetTotal(out Money total))
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.AmountOverflow, remaining);
                    }

                    if (total == Money.Zero)
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.AmountMustBePositive, remaining);
                    }

                    if (total > remaining)
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.ExceedsRemainingDebt, remaining);
                    }

                    if (allocation.FromAvailable > wallet.Available)
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.InsufficientAvailableFunds, remaining);
                    }

                    if (allocation.FromProtected > wallet.Protected)
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.InsufficientProtectedFunds, remaining);
                    }

                    if (!wallet.TryDebitAllocated(allocation.FromAvailable, allocation.FromProtected))
                    {
                        return DebtPaymentResult.Failed(DebtPaymentFailure.WalletChanged, remaining);
                    }

                    remaining -= total;
                    return DebtPaymentResult.Success(total, remaining);
                }
            }
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

    }
}
