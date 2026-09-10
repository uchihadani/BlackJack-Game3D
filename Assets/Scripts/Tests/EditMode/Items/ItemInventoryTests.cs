using NUnit.Framework;
using TwentyThree.Domain.Items;

namespace TwentyThree.Tests.EditMode.Items
{
    public sealed class ItemInventoryTests
    {
        private IItemCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = new DemoItemCatalogFactory().Create();
        }

        [Test]
        public void DemoInventoryStartsWithThreeChargeIt04OnTheTable()
        {
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();

            Assert.That(inventory.TableItems, Has.Count.EqualTo(1));
            Assert.That(inventory.StoredItems, Is.Empty);
            Assert.That(inventory.TryGetItem(ItemId.IT04, out ItemInstance item, out InventoryLocation location), Is.True);
            Assert.That(location, Is.EqualTo(InventoryLocation.Table));
            Assert.That(item.ChargesRemaining, Is.EqualTo(3));
        }

        [Test]
        public void InventoryEnforcesTableCapacityAndGlobalDuplicateRule()
        {
            ItemInventory inventory = new ItemInventory();

            Assert.That(Add(inventory, ItemId.IT01, InventoryLocation.Table).Succeeded, Is.True);
            Assert.That(Add(inventory, ItemId.IT02, InventoryLocation.Table).Succeeded, Is.True);
            Assert.That(Add(inventory, ItemId.IT03, InventoryLocation.Table).Succeeded, Is.True);
            Assert.That(Add(inventory, ItemId.IT04, InventoryLocation.Table).Succeeded, Is.True);

            InventoryOperationResult full = Add(inventory, ItemId.IT05, InventoryLocation.Table);
            InventoryOperationResult duplicate = Add(inventory, ItemId.IT01, InventoryLocation.Storage);

            Assert.That(full.Failure, Is.EqualTo(InventoryOperationFailure.DestinationFull));
            Assert.That(duplicate.Failure, Is.EqualTo(InventoryOperationFailure.AlreadyOwned));
            Assert.That(inventory.Count(InventoryLocation.Table), Is.EqualTo(ItemInventory.TableCapacity));
            Assert.That(inventory.Count(InventoryLocation.Storage), Is.Zero);
        }

        [Test]
        public void InventoryEnforcesStorageCapacity()
        {
            ItemInventory inventory = new ItemInventory();

            Assert.That(Add(inventory, ItemId.IT01, InventoryLocation.Storage).Succeeded, Is.True);
            Assert.That(Add(inventory, ItemId.IT02, InventoryLocation.Storage).Succeeded, Is.True);

            InventoryOperationResult result = Add(inventory, ItemId.IT03, InventoryLocation.Storage);

            Assert.That(result.Failure, Is.EqualTo(InventoryOperationFailure.DestinationFull));
            Assert.That(inventory.Count(InventoryLocation.Storage), Is.EqualTo(ItemInventory.StorageCapacity));
        }

        [Test]
        public void MoveUsesAFreeDestinationWithoutChangingCharges()
        {
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            inventory.TryConsumeTableCharge(ItemId.IT04);

            InventoryOperationResult result = inventory.TryMove(ItemId.IT04, InventoryLocation.Storage);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Item.ChargesRemaining, Is.EqualTo(2));
            Assert.That(inventory.TableItems, Is.Empty);
            Assert.That(inventory.TryGetItem(ItemId.IT04, out _, out InventoryLocation location), Is.True);
            Assert.That(location, Is.EqualTo(InventoryLocation.Storage));
        }

        [Test]
        public void FullDestinationRequiresAnExplicitAtomicSwap()
        {
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            Add(inventory, ItemId.IT01, InventoryLocation.Storage);
            Add(inventory, ItemId.IT02, InventoryLocation.Storage);

            InventoryOperationResult move = inventory.TryMove(ItemId.IT04, InventoryLocation.Storage);

            Assert.That(move.Failure, Is.EqualTo(InventoryOperationFailure.DestinationFull));
            AssertLocation(inventory, ItemId.IT04, InventoryLocation.Table);
            AssertLocation(inventory, ItemId.IT01, InventoryLocation.Storage);

            InventoryOperationResult swap = inventory.TrySwap(ItemId.IT04, ItemId.IT01);

            Assert.That(swap.Succeeded, Is.True);
            AssertLocation(inventory, ItemId.IT04, InventoryLocation.Storage);
            AssertLocation(inventory, ItemId.IT01, InventoryLocation.Table);
            Assert.That(inventory.Count(InventoryLocation.Table), Is.EqualTo(1));
            Assert.That(inventory.Count(InventoryLocation.Storage), Is.EqualTo(2));
        }

        [Test]
        public void SwapIsRejectedWithoutMutationWhenDestinationHasSpace()
        {
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            Add(inventory, ItemId.IT01, InventoryLocation.Storage);

            InventoryOperationResult result = inventory.TrySwap(ItemId.IT04, ItemId.IT01);

            Assert.That(result.Failure, Is.EqualTo(InventoryOperationFailure.SwapRequiresFullDestination));
            AssertLocation(inventory, ItemId.IT04, InventoryLocation.Table);
            AssertLocation(inventory, ItemId.IT01, InventoryLocation.Storage);
        }

        [Test]
        public void DepletedConsumableIsRemovedButIt04RemainsAsAContainer()
        {
            ItemInventory inventory = new DemoItemInventoryFactory(catalog).Create();
            Add(inventory, ItemId.IT01, InventoryLocation.Table);

            InventoryOperationResult consumable = inventory.TryConsumeTableCharge(ItemId.IT01);
            Assert.That(consumable.Succeeded, Is.True);
            Assert.That(consumable.Item.IsDepleted, Is.True);
            Assert.That(inventory.Contains(ItemId.IT01), Is.False);

            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);
            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);
            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Succeeded, Is.True);

            Assert.That(inventory.TryGetItem(ItemId.IT04, out ItemInstance container, out _), Is.True);
            Assert.That(container.IsDepleted, Is.True);
            Assert.That(inventory.TryConsumeTableCharge(ItemId.IT04).Failure, Is.EqualTo(InventoryOperationFailure.NoChargesRemaining));
        }

        [Test]
        public void StoredItemCannotConsumeACharge()
        {
            ItemInventory inventory = new ItemInventory();
            Add(inventory, ItemId.IT01, InventoryLocation.Storage);

            InventoryOperationResult result = inventory.TryConsumeTableCharge(ItemId.IT01);

            Assert.That(result.Failure, Is.EqualTo(InventoryOperationFailure.ItemNotOnTable));
            Assert.That(inventory.StoredItems[0].ChargesRemaining, Is.EqualTo(1));
        }

        [Test]
        public void RestoreRejectsImpossibleChargeStateWithoutAddingAnItem()
        {
            ItemInventory inventory = new ItemInventory();
            Assert.That(catalog.TryGetDefinition(ItemId.IT01, out ItemDefinition definition), Is.True);

            InventoryOperationResult depleted = inventory.TryRestore(
                definition,
                0,
                InventoryLocation.Table);
            InventoryOperationResult excessive = inventory.TryRestore(
                definition,
                2,
                InventoryLocation.Table);

            Assert.That(depleted.Failure, Is.EqualTo(InventoryOperationFailure.InvalidChargeCount));
            Assert.That(excessive.Failure, Is.EqualTo(InventoryOperationFailure.InvalidChargeCount));
            Assert.That(inventory.Contains(ItemId.IT01), Is.False);
        }

        private InventoryOperationResult Add(
            ItemInventory inventory,
            ItemId itemId,
            InventoryLocation location)
        {
            Assert.That(catalog.TryGetDefinition(itemId, out ItemDefinition definition), Is.True);
            return inventory.TryAdd(definition, location);
        }

        private static void AssertLocation(
            ItemInventory inventory,
            ItemId itemId,
            InventoryLocation expected)
        {
            Assert.That(inventory.TryGetItem(itemId, out _, out InventoryLocation actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
