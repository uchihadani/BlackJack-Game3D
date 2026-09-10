using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Items
{
    public enum ItemPurchaseFailure
    {
        None = 0,
        UnknownItem = 1,
        InvalidDestination = 2,
        AlreadyOwned = 3,
        DestinationFull = 4,
        UnavailableAfterActivation = 5,
        InsufficientAvailableFunds = 6,
        InventoryChanged = 7
    }

    public readonly struct ItemPurchaseContext
    {
        public ItemPurchaseContext(bool wasActivatedDuringRun)
        {
            WasActivatedDuringRun = wasActivatedDuringRun;
        }

        public bool WasActivatedDuringRun { get; }
    }

    public readonly struct ItemPurchaseResult
    {
        private ItemPurchaseResult(
            ItemInstance item,
            Money price,
            ItemPurchaseFailure failure)
        {
            Item = item;
            Price = price;
            Failure = failure;
        }

        public bool Succeeded => Failure == ItemPurchaseFailure.None;

        public ItemInstance Item { get; }

        public Money Price { get; }

        public ItemPurchaseFailure Failure { get; }

        internal static ItemPurchaseResult Success(ItemInstance item, Money price)
        {
            return new ItemPurchaseResult(item, price, ItemPurchaseFailure.None);
        }

        internal static ItemPurchaseResult Failed(ItemPurchaseFailure failure, Money price)
        {
            return new ItemPurchaseResult(null, price, failure);
        }
    }
}
