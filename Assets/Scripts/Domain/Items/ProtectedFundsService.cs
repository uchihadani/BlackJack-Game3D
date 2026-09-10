using System;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Items
{
    public enum ProtectedFundsTransferFailure
    {
        None = 0,
        AmountMustBePositive = 1,
        ContainerUnavailable = 2,
        InsufficientAvailableFunds = 3,
        InsufficientProtectedFunds = 4,
        CapacityExceeded = 5,
        BalanceOverflow = 6
    }

    public readonly struct ProtectedFundsTransferResult
    {
        private ProtectedFundsTransferResult(
            ProtectedFundsTransferFailure failure,
            Money available,
            Money protectedFunds)
        {
            Failure = failure;
            Available = available;
            Protected = protectedFunds;
        }

        public bool Succeeded => Failure == ProtectedFundsTransferFailure.None;

        public ProtectedFundsTransferFailure Failure { get; }

        public Money Available { get; }

        public Money Protected { get; }

        internal static ProtectedFundsTransferResult Success(Wallet wallet)
        {
            return new ProtectedFundsTransferResult(
                ProtectedFundsTransferFailure.None,
                wallet.Available,
                wallet.Protected);
        }

        internal static ProtectedFundsTransferResult Failed(
            ProtectedFundsTransferFailure failure,
            Wallet wallet)
        {
            return new ProtectedFundsTransferResult(failure, wallet.Available, wallet.Protected);
        }
    }

    public sealed class ProtectedFundsService
    {
        private readonly Money _containerCapacity;

        public static Money ContainerCapacity => Money.FromCoins(20);

        public ProtectedFundsService()
            : this(ContainerCapacity)
        {
        }

        public ProtectedFundsService(Money containerCapacity)
        {
            if (containerCapacity == Money.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(containerCapacity));
            }

            _containerCapacity = containerCapacity;
        }

        public Money GetCapacity(ItemInventory inventory)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            return inventory.Contains(ItemId.IT04) ? _containerCapacity : Money.Zero;
        }

        public ProtectedFundsTransferResult TryDeposit(
            Wallet wallet,
            ItemInventory inventory,
            Money amount)
        {
            ValidateArguments(wallet, inventory);

            lock (inventory.SynchronizationRoot)
            {
                lock (wallet.SynchronizationRoot)
                {
                    if (amount == Money.Zero)
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.AmountMustBePositive,
                            wallet);
                    }

                    if (!inventory.Contains(ItemId.IT04))
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.ContainerUnavailable,
                            wallet);
                    }

                    if (amount > wallet.Available)
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.InsufficientAvailableFunds,
                            wallet);
                    }

                    Money capacity = _containerCapacity;

                    if (wallet.Protected > capacity || amount > capacity - wallet.Protected)
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.CapacityExceeded,
                            wallet);
                    }

                    if (!wallet.TryTransferAvailableToProtected(amount, capacity))
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.CapacityExceeded,
                            wallet);
                    }

                    return ProtectedFundsTransferResult.Success(wallet);
                }
            }
        }

        public ProtectedFundsTransferResult TryWithdraw(
            Wallet wallet,
            ItemInventory inventory,
            Money amount)
        {
            ValidateArguments(wallet, inventory);

            lock (inventory.SynchronizationRoot)
            {
                lock (wallet.SynchronizationRoot)
                {
                    if (amount == Money.Zero)
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.AmountMustBePositive,
                            wallet);
                    }

                    if (!inventory.Contains(ItemId.IT04))
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.ContainerUnavailable,
                            wallet);
                    }

                    if (amount > wallet.Protected)
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.InsufficientProtectedFunds,
                            wallet);
                    }

                    if (!wallet.TryTransferProtectedToAvailable(amount))
                    {
                        return ProtectedFundsTransferResult.Failed(
                            ProtectedFundsTransferFailure.BalanceOverflow,
                            wallet);
                    }

                    return ProtectedFundsTransferResult.Success(wallet);
                }
            }
        }

        private static void ValidateArguments(Wallet wallet, ItemInventory inventory)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }
        }
    }
}
