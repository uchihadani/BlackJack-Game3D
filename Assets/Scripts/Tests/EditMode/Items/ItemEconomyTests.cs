using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Items;

namespace TwentyThree.Tests.EditMode.Items
{
    public sealed class ItemEconomyTests
    {
        private IItemCatalog catalog;
        private ItemCommerceService commerce;

        [SetUp]
        public void SetUp()
        {
            catalog = new DemoItemCatalogFactory().Create();
            commerce = new ItemCommerceService(catalog);
        }

        [Test]
        public void PurchaseAtomicallyDebitsAvailableFundsAndCreatesTheItem()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();

            ItemPurchaseResult result = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT01,
                InventoryLocation.Table);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Price, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(result.Item.Id, Is.EqualTo(ItemId.IT01));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(10)));
            Assert.That(inventory.Contains(ItemId.IT01), Is.True);
        }

        [Test]
        public void InvalidPurchasesNeverDebitAvailableFunds()
        {
            Wallet wallet = new Wallet(Money.FromCoins(100));
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();

            ItemPurchaseResult duplicate = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT04,
                InventoryLocation.Storage);
            ItemPurchaseResult unknown = commerce.TryPurchase(
                wallet,
                inventory,
                (ItemId)99,
                InventoryLocation.Table);

            Assert.That(duplicate.Failure, Is.EqualTo(ItemPurchaseFailure.AlreadyOwned));
            Assert.That(unknown.Failure, Is.EqualTo(ItemPurchaseFailure.UnknownItem));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(100)));
            Assert.That(inventory.TableItems, Has.Count.EqualTo(1));
            Assert.That(inventory.StoredItems, Is.Empty);
        }

        [Test]
        public void PurchaseFailsAtomicallyWhenDestinationOrFundsAreInsufficient()
        {
            Wallet wallet = new Wallet(Money.FromCoins(30));
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            Add(inventory, ItemId.IT01, InventoryLocation.Table);
            Add(inventory, ItemId.IT02, InventoryLocation.Table);
            Add(inventory, ItemId.IT03, InventoryLocation.Table);

            ItemPurchaseResult full = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT05,
                InventoryLocation.Table);
            ItemPurchaseResult insufficient = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT05,
                InventoryLocation.Storage);

            Assert.That(full.Failure, Is.EqualTo(ItemPurchaseFailure.DestinationFull));
            Assert.That(insufficient.Failure, Is.EqualTo(ItemPurchaseFailure.InsufficientAvailableFunds));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(30)));
            Assert.That(inventory.Contains(ItemId.IT05), Is.False);
        }

        [Test]
        public void It02CostsFortyAfterItsFirstRunActivation()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            ItemInventory inventory = new ItemInventory();

            ItemPurchaseResult result = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT02,
                InventoryLocation.Table,
                new ItemPurchaseContext(true));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Price, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(10)));
        }

        [Test]
        public void It03CannotBeRepurchasedAfterItsRunActivation()
        {
            Wallet wallet = new Wallet(Money.FromCoins(100));
            ItemInventory inventory = new ItemInventory();

            ItemPurchaseResult result = commerce.TryPurchase(
                wallet,
                inventory,
                ItemId.IT03,
                InventoryLocation.Table,
                new ItemPurchaseContext(true));

            Assert.That(result.Failure, Is.EqualTo(ItemPurchaseFailure.UnavailableAfterActivation));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(100)));
            Assert.That(inventory.Contains(ItemId.IT03), Is.False);
        }

        [Test]
        public void DiscardNeverRefundsAndIt04RequiresAnEmptyProtectedBalance()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            ProtectedFundsService protectedFunds = new ProtectedFundsService();
            Assert.That(protectedFunds.TryDeposit(wallet, inventory, Money.FromCoins(10)).Succeeded, Is.True);

            InventoryOperationResult blocked = commerce.TryDiscard(wallet, inventory, ItemId.IT04);

            Assert.That(blocked.Failure, Is.EqualTo(InventoryOperationFailure.ProtectedFundsPresent));
            Assert.That(inventory.Contains(ItemId.IT04), Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(10)));

            Assert.That(protectedFunds.TryWithdraw(wallet, inventory, Money.FromCoins(10)).Succeeded, Is.True);
            InventoryOperationResult discarded = commerce.TryDiscard(wallet, inventory, ItemId.IT04);

            Assert.That(discarded.Succeeded, Is.True);
            Assert.That(inventory.Contains(ItemId.IT04), Is.False);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
        }

        private void Add(ItemInventory inventory, ItemId itemId, InventoryLocation location)
        {
            Assert.That(catalog.TryGetDefinition(itemId, out ItemDefinition definition), Is.True);
            Assert.That(inventory.TryAdd(definition, location).Succeeded, Is.True);
        }
    }
}
