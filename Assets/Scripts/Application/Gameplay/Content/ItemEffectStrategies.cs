using TwentyThree.Domain.Items;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class MagnifyingGlassStrategy : IItemEffectStrategy
    {
        public ItemId Id => ItemId.IT01;

        public ItemActivationValidation Validate(IItemEffectHost host)
        {
            if (host.Phase != GamePhase.PlayerTurn)
            {
                return ItemActivationValidation.Failed(
                    ItemActivationFailure.InvalidPhase);
            }

            if (host.ItemsBlockedForCurrentRound)
            {
                return ItemActivationValidation.Failed(ItemActivationFailure.ItemBlocked);
            }

            if (host.DealerHoleCardRevealed || host.BlackoutActive || host.Pressure >= 70)
            {
                return ItemActivationValidation.Failed(
                    ItemActivationFailure.PreconditionsNotMet);
            }

            return ItemActivationValidation.Success();
        }

        public void Apply(IItemEffectHost host)
        {
            host.RevealDealerHoleCardWithMagnifyingGlass();
        }
    }

    internal sealed class CigaretteBoxStrategy : IItemEffectStrategy
    {
        public ItemId Id => ItemId.IT04;

        public ItemActivationValidation Validate(IItemEffectHost host)
        {
            if (host.Phase != GamePhase.PlayerTurn && !host.IsPreparationWindowOpen)
            {
                return ItemActivationValidation.Failed(
                    ItemActivationFailure.InvalidPhase);
            }

            if (host.ItemsBlockedForCurrentRound)
            {
                return ItemActivationValidation.Failed(ItemActivationFailure.ItemBlocked);
            }

            if (host.Pressure == 0)
            {
                return ItemActivationValidation.Failed(
                    ItemActivationFailure.PreconditionsNotMet);
            }

            return ItemActivationValidation.Success();
        }

        public void Apply(IItemEffectHost host)
        {
            host.ApplyCigaretteBox();
        }
    }
}
