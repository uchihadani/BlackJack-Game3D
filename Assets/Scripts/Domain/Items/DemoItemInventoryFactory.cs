using System;

namespace TwentyThree.Domain.Items
{
    public sealed class DemoItemInventoryFactory
    {
        private readonly IItemCatalog catalog;

        public DemoItemInventoryFactory(IItemCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public ItemInventory Create()
        {
            if (!catalog.TryGetDefinition(ItemId.IT04, out ItemDefinition definition))
            {
                throw new InvalidOperationException("The demo catalog must define IT-04.");
            }

            ItemInventory inventory = new ItemInventory();
            InventoryOperationResult result = inventory.TryAdd(definition, InventoryLocation.Table);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException("The initial item inventory could not be created.");
            }

            return inventory;
        }
    }
}
