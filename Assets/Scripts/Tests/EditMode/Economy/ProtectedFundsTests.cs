using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Items;

namespace TwentyThree.Tests.EditMode.Economy
{
    public sealed class ProtectedFundsTests
    {
        private IItemCatalog catalog;
        private ItemInventory inventory;
        private ProtectedFundsService service;

        [SetUp]
        public void SetUp()
        {
            catalog = new DemoItemCatalogFactory().Create();
            inventory = new DemoItemInventoryFactory(catalog).Create();
            service = new ProtectedFundsService();
        }

        [Test]
        public void DepositAndWithdrawalAtomicallyTransferBalances()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));

            ProtectedFundsTransferResult deposit = service.TryDeposit(
                wallet,
                inventory,
                Money.FromCoins(15));
            ProtectedFundsTransferResult withdrawal = service.TryWithdraw(
                wallet,
                inventory,
                Money.FromCoins(5));

            Assert.That(deposit.Succeeded, Is.True);
            Assert.That(deposit.Available, Is.EqualTo(Money.FromCoins(35)));
            Assert.That(deposit.Protected, Is.EqualTo(Money.FromCoins(15)));
            Assert.That(withdrawal.Succeeded, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(10)));
        }

        [Test]
        public void DepositCannotExceedTwentyCoinCapacity()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            Assert.That(service.TryDeposit(wallet, inventory, Money.FromCoins(20)).Succeeded, Is.True);

            ProtectedFundsTransferResult result = service.TryDeposit(
                wallet,
                inventory,
                Money.FromMinorUnits(1));

            Assert.That(result.Failure, Is.EqualTo(ProtectedFundsTransferFailure.CapacityExceeded));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(30)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(20)));
        }

        [Test]
        public void InvalidTransfersDoNotChangeEitherBalance()
        {
            Wallet wallet = new Wallet(Money.FromCoins(5));

            ProtectedFundsTransferResult zero = service.TryDeposit(wallet, inventory, Money.Zero);
            ProtectedFundsTransferResult unavailable = service.TryDeposit(wallet, inventory, Money.FromCoins(6));
            ProtectedFundsTransferResult withdrawal = service.TryWithdraw(wallet, inventory, Money.FromCoins(1));

            Assert.That(zero.Failure, Is.EqualTo(ProtectedFundsTransferFailure.AmountMustBePositive));
            Assert.That(unavailable.Failure, Is.EqualTo(ProtectedFundsTransferFailure.InsufficientAvailableFunds));
            Assert.That(withdrawal.Failure, Is.EqualTo(ProtectedFundsTransferFailure.InsufficientProtectedFunds));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(5)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.Zero));
        }

        [Test]
        public void It04ProvidesCapacityFromStorageAndAtZeroCharges()
        {
            Wallet wallet = new Wallet(Money.FromCoins(20));

            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);
            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);
            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);
            Assert.That(inventory.TryMove(ItemId.IT04, InventoryLocation.Storage).Succeeded, Is.True);

            Assert.That(service.GetCapacity(inventory), Is.EqualTo(Money.FromCoins(20)));
            Assert.That(service.TryDeposit(wallet, inventory, Money.FromCoins(20)).Succeeded, Is.True);
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(20)));
        }

        [Test]
        public void TransfersRequireTheIt04Container()
        {
            Wallet wallet = new Wallet(Money.FromCoins(20));
            ItemInventory emptyInventory = new ItemInventory();

            ProtectedFundsTransferResult result = service.TryDeposit(
                wallet,
                emptyInventory,
                Money.FromCoins(1));

            Assert.That(result.Failure, Is.EqualTo(ProtectedFundsTransferFailure.ContainerUnavailable));
            Assert.That(service.GetCapacity(emptyInventory), Is.EqualTo(Money.Zero));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(20)));
        }

        [Test]
        public void FailedOverflowingWithdrawalLeavesBothBalancesUnchanged()
        {
            Wallet wallet = new Wallet(
                Money.FromMinorUnits(long.MaxValue),
                Money.FromMinorUnits(1));

            ProtectedFundsTransferResult result = service.TryWithdraw(
                wallet,
                inventory,
                Money.FromMinorUnits(1));

            Assert.That(result.Failure, Is.EqualTo(ProtectedFundsTransferFailure.BalanceOverflow));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(long.MaxValue)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromMinorUnits(1)));
        }
    }
}
