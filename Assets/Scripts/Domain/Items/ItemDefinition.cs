using System;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Items
{
    public enum ItemActivationMode
    {
        Manual = 0,
        Automatic = 1
    }

    public sealed class ItemDefinition
    {
        public ItemDefinition(
            ItemId id,
            string displayName,
            Money basePurchasePrice,
            Money purchasePriceAfterActivation,
            int initialCharges,
            ItemActivationMode activationMode,
            bool remainsWhenDepleted,
            bool grantedAtRunStart,
            bool canPurchaseAfterActivation)
        {
            if (!id.IsDemoItem())
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (initialCharges <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCharges));
            }

            if (!Enum.IsDefined(typeof(ItemActivationMode), activationMode))
            {
                throw new ArgumentOutOfRangeException(nameof(activationMode));
            }

            Id = id;
            DisplayName = displayName;
            BasePurchasePrice = basePurchasePrice;
            PurchasePriceAfterActivation = purchasePriceAfterActivation;
            InitialCharges = initialCharges;
            ActivationMode = activationMode;
            RemainsWhenDepleted = remainsWhenDepleted;
            GrantedAtRunStart = grantedAtRunStart;
            CanPurchaseAfterActivation = canPurchaseAfterActivation;
        }

        public ItemId Id { get; }

        public string TechnicalCode => Id.ToTechnicalCode();

        public string DisplayName { get; }

        public Money BasePurchasePrice { get; }

        public Money PurchasePriceAfterActivation { get; }

        public int InitialCharges { get; }

        public ItemActivationMode ActivationMode { get; }

        public bool RemainsWhenDepleted { get; }

        public bool GrantedAtRunStart { get; }

        public bool CanPurchaseAfterActivation { get; }

        public Money ResolvePurchasePrice(bool wasActivatedDuringRun)
        {
            return wasActivatedDuringRun ? PurchasePriceAfterActivation : BasePurchasePrice;
        }
    }
}
