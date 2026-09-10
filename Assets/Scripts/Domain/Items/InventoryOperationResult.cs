namespace TwentyThree.Domain.Items
{
    public enum InventoryOperationFailure
    {
        None = 0,
        InvalidLocation = 1,
        AlreadyOwned = 2,
        ItemNotOwned = 3,
        ItemNotOnTable = 4,
        DestinationIsCurrentLocation = 5,
        DestinationFull = 6,
        SwapRequiresFullDestination = 7,
        SwapTargetNotInDestination = 8,
        NoChargesRemaining = 9,
        ProtectedFundsPresent = 10,
        InvalidChargeCount = 11
    }

    public readonly struct InventoryOperationResult
    {
        private InventoryOperationResult(ItemInstance item, InventoryOperationFailure failure)
        {
            Item = item;
            Failure = failure;
        }

        public bool Succeeded => Failure == InventoryOperationFailure.None;

        public ItemInstance Item { get; }

        public InventoryOperationFailure Failure { get; }

        internal static InventoryOperationResult Success(ItemInstance item)
        {
            return new InventoryOperationResult(item, InventoryOperationFailure.None);
        }

        internal static InventoryOperationResult Failed(InventoryOperationFailure failure)
        {
            return new InventoryOperationResult(null, failure);
        }
    }
}
