using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Domain.Items
{
    public sealed class ItemInventory
    {
        public const int TableCapacity = 4;
        public const int StorageCapacity = 2;

        private readonly object sync = new object();
        private readonly ItemInstance[] table = new ItemInstance[TableCapacity];
        private readonly ItemInstance[] storage = new ItemInstance[StorageCapacity];

        internal object SynchronizationRoot => sync;

        public IReadOnlyList<ItemInstance> TableItems => GetItems(InventoryLocation.Table);

        public IReadOnlyList<ItemInstance> StoredItems => GetItems(InventoryLocation.Storage);

        public int Count(InventoryLocation location)
        {
            lock (sync)
            {
                ItemInstance[] slots = GetSlots(location);
                int count = 0;

                for (int index = 0; index < slots.Length; index++)
                {
                    if (slots[index] != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool Contains(ItemId itemId)
        {
            lock (sync)
            {
                return TryFindUnsafe(itemId, out _, out _);
            }
        }

        public bool TryGetItem(ItemId itemId, out ItemInstance item, out InventoryLocation location)
        {
            lock (sync)
            {
                return TryFindUnsafe(itemId, out item, out location);
            }
        }

        public InventoryOperationResult TryAdd(ItemDefinition definition, InventoryLocation location)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return TryRestore(definition, definition.InitialCharges, location);
        }

        public InventoryOperationResult TryRestore(
            ItemDefinition definition,
            int chargesRemaining,
            InventoryLocation location)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            lock (sync)
            {
                if (chargesRemaining < 0 ||
                    chargesRemaining > definition.InitialCharges ||
                    chargesRemaining == 0 && !definition.RemainsWhenDepleted)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.InvalidChargeCount);
                }

                InventoryOperationResult validation = ValidateAddUnsafe(definition.Id, location);

                if (!validation.Succeeded)
                {
                    return validation;
                }

                ItemInstance[] slots = GetSlots(location);
                int emptyIndex = FindEmptySlot(slots);
                ItemInstance item = new ItemInstance(definition, chargesRemaining);
                slots[emptyIndex] = item;
                return InventoryOperationResult.Success(item);
            }
        }

        public InventoryOperationResult TryMove(ItemId itemId, InventoryLocation destination)
        {
            lock (sync)
            {
                if (!IsValidLocation(destination))
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.InvalidLocation);
                }

                if (!TryFindUnsafe(itemId, out ItemInstance item, out InventoryLocation source))
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOwned);
                }

                if (source == destination)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.DestinationIsCurrentLocation);
                }

                ItemInstance[] destinationSlots = GetSlots(destination);
                int destinationIndex = FindEmptySlot(destinationSlots);

                if (destinationIndex < 0)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.DestinationFull);
                }

                ItemInstance[] sourceSlots = GetSlots(source);
                int sourceIndex = FindItemSlot(sourceSlots, itemId);
                sourceSlots[sourceIndex] = null;
                destinationSlots[destinationIndex] = item;
                return InventoryOperationResult.Success(item);
            }
        }

        public InventoryOperationResult TrySwap(ItemId movingItemId, ItemId destinationItemId)
        {
            lock (sync)
            {
                if (!TryFindUnsafe(movingItemId, out ItemInstance movingItem, out InventoryLocation source))
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOwned);
                }

                if (!TryFindUnsafe(destinationItemId, out ItemInstance destinationItem, out InventoryLocation destination) ||
                    source == destination)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.SwapTargetNotInDestination);
                }

                ItemInstance[] destinationSlots = GetSlots(destination);

                if (FindEmptySlot(destinationSlots) >= 0)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.SwapRequiresFullDestination);
                }

                ItemInstance[] sourceSlots = GetSlots(source);
                int sourceIndex = FindItemSlot(sourceSlots, movingItemId);
                int destinationIndex = FindItemSlot(destinationSlots, destinationItemId);
                sourceSlots[sourceIndex] = destinationItem;
                destinationSlots[destinationIndex] = movingItem;
                return InventoryOperationResult.Success(movingItem);
            }
        }

        public InventoryOperationResult TryConsumeTableCharge(ItemId itemId)
        {
            lock (sync)
            {
                if (!TryFindUnsafe(itemId, out ItemInstance item, out InventoryLocation location))
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOwned);
                }

                if (location != InventoryLocation.Table)
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOnTable);
                }

                if (!item.TryConsumeCharge())
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.NoChargesRemaining);
                }

                if (item.IsDepleted && !item.Definition.RemainsWhenDepleted)
                {
                    table[FindItemSlot(table, itemId)] = null;
                }

                return InventoryOperationResult.Success(item);
            }
        }

        internal InventoryOperationResult ValidateAdd(ItemId itemId, InventoryLocation location)
        {
            lock (sync)
            {
                return ValidateAddUnsafe(itemId, location);
            }
        }

        internal InventoryOperationResult TryRemove(ItemId itemId)
        {
            lock (sync)
            {
                if (!TryFindUnsafe(itemId, out ItemInstance item, out InventoryLocation location))
                {
                    return InventoryOperationResult.Failed(InventoryOperationFailure.ItemNotOwned);
                }

                ItemInstance[] slots = GetSlots(location);
                slots[FindItemSlot(slots, itemId)] = null;
                return InventoryOperationResult.Success(item);
            }
        }

        private IReadOnlyList<ItemInstance> GetItems(InventoryLocation location)
        {
            lock (sync)
            {
                ItemInstance[] slots = GetSlots(location);
                List<ItemInstance> items = new List<ItemInstance>(slots.Length);

                for (int index = 0; index < slots.Length; index++)
                {
                    if (slots[index] != null)
                    {
                        items.Add(slots[index]);
                    }
                }

                return new ReadOnlyCollection<ItemInstance>(items);
            }
        }

        private InventoryOperationResult ValidateAddUnsafe(ItemId itemId, InventoryLocation location)
        {
            if (!IsValidLocation(location))
            {
                return InventoryOperationResult.Failed(InventoryOperationFailure.InvalidLocation);
            }

            if (TryFindUnsafe(itemId, out _, out _))
            {
                return InventoryOperationResult.Failed(InventoryOperationFailure.AlreadyOwned);
            }

            if (FindEmptySlot(GetSlots(location)) < 0)
            {
                return InventoryOperationResult.Failed(InventoryOperationFailure.DestinationFull);
            }

            return InventoryOperationResult.Success(null);
        }

        private bool TryFindUnsafe(
            ItemId itemId,
            out ItemInstance item,
            out InventoryLocation location)
        {
            int tableIndex = FindItemSlot(table, itemId);

            if (tableIndex >= 0)
            {
                item = table[tableIndex];
                location = InventoryLocation.Table;
                return true;
            }

            int storageIndex = FindItemSlot(storage, itemId);

            if (storageIndex >= 0)
            {
                item = storage[storageIndex];
                location = InventoryLocation.Storage;
                return true;
            }

            item = null;
            location = default;
            return false;
        }

        private ItemInstance[] GetSlots(InventoryLocation location)
        {
            switch (location)
            {
                case InventoryLocation.Table:
                    return table;
                case InventoryLocation.Storage:
                    return storage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location));
            }
        }

        private static bool IsValidLocation(InventoryLocation location)
        {
            return location == InventoryLocation.Table || location == InventoryLocation.Storage;
        }

        private static int FindEmptySlot(ItemInstance[] slots)
        {
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] == null)
                {
                    return index;
                }
            }

            return -1;
        }

        private static int FindItemSlot(ItemInstance[] slots, ItemId itemId)
        {
            for (int index = 0; index < slots.Length; index++)
            {
                if (slots[index] != null && slots[index].Id == itemId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
