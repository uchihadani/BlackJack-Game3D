using System;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Items
{
    public sealed class ItemCommerceService
    {
        private readonly IItemCatalog catalog;

        public ItemCommerceService(IItemCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public ItemPurchaseResult TryPurchase(
            Wallet wallet,
            ItemInventory inventory,
            ItemId itemId,
            InventoryLocation destination)
        {
            return TryPurchase(
                wallet,
                inventory,
                itemId,
                destination,
                new ItemPurchaseContext(false));
        }

        public ItemPurchaseResult TryPurchase(
            Wallet wallet,
            ItemInventory inventory,
            ItemId itemId,
            InventoryLocation destination,
            ItemPurchaseContext context)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (!catalog.TryGetDefinition(itemId, out ItemDefinition definition))
            {
                return ItemPurchaseResult.Failed(ItemPurchaseFailure.UnknownItem, Money.Zero);
            }

            Money price = definition.ResolvePurchasePrice(context.WasActivatedDuringRun);

            lock (inventory.SynchronizationRoot)
            {
                lock (wallet.SynchronizationRoot)
                {
                    InventoryOperationResult validation = inventory.ValidateAdd(itemId, destination);

                    if (!validation.Succeeded)
                    {
                        return ItemPurchaseResult.Failed(MapInventoryFailure(validation.Failure), price);
                    }

                    if (context.WasActivatedDuringRun && !definition.CanPurchaseAfterActivation)
                    {
                        return ItemPurchaseResult.Failed(ItemPurchaseFailure.UnavailableAfterActivation, price);
                    }

                    if (wallet.Available < price)
                    {
                        return ItemPurchaseResult.Failed(ItemPurchaseFailure.InsufficientAvailableFunds, price);
                    }

                    if (!wallet.TryDebitAvailable(price))
                    {
                        return ItemPurchaseResult.Failed(ItemPurchaseFailure.InsufficientAvailableFunds, price);
                    }

                    InventoryOperationResult addition = inventory.TryAdd(definition, destination);

                    if (!addition.Succeeded)
                    {
                        wallet.CreditAvailable(price);
                        return ItemPurchaseResult.Failed(ItemPurchaseFailure.InventoryChanged, price);
                    }

                    return ItemPurchaseResult.Success(addition.Item, price);
                }
            }
        }

        public InventoryOperationResult TryDiscard(
            Wallet wallet,
            ItemInventory inventory,
            ItemId itemId)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            lock (inventory.SynchronizationRoot)
            {
                lock (wallet.SynchronizationRoot)
                {
                    if (!inventory.Contains(itemId))
                    {
                        return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOwned);
                    }

                    if (itemId == ItemId.IT04 && wallet.Protected > Money.Zero)
                    {
                        return InventoryOperationResult.Failed(InventoryOperationFailure.ProtectedFundsPresent);
                    }

                    return inventory.TryRemove(itemId);
                }
            }
        }

        private static ItemPurchaseFailure MapInventoryFailure(InventoryOperationFailure failure)
        {
            switch (failure)
            {
                case InventoryOperationFailure.InvalidLocation:
                    return ItemPurchaseFailure.InvalidDestination;
                case InventoryOperationFailure.AlreadyOwned:
                    return ItemPurchaseFailure.AlreadyOwned;
                case InventoryOperationFailure.DestinationFull:
                    return ItemPurchaseFailure.DestinationFull;
                default:
                    return ItemPurchaseFailure.InventoryChanged;
            }
        }
    }
}
