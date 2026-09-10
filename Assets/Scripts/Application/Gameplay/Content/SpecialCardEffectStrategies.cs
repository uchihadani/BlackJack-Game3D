using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay.Content
{
    internal sealed class BeginnersLuckStrategy : ISpecialCardEffectStrategy
    {
        public SpecialCardId Id => SpecialCardId.BeginnersLuck;

        public bool CanResolve(ISpecialCardEffectHost host)
        {
            return host.RemainingDrawPileCount >= 2;
        }

        public void Accept(ISpecialCardEffectHost host)
        {
            host.ReshuffleRemainingDrawPile();
        }

        public void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid)
        {
        }
    }

    internal sealed class PanicAttackStrategy : ISpecialCardEffectStrategy
    {
        public SpecialCardId Id => SpecialCardId.PanicAttack;

        public bool CanResolve(ISpecialCardEffectHost host)
        {
            return true;
        }

        public void Accept(ISpecialCardEffectHost host)
        {
            host.BlockItemsForCurrentRound();
            host.ApplyPressure(
                PressureChangeCause.Sc02PanicAttackResolved,
                host.CurrentSpecialPressureDelta);
        }

        public void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid)
        {
        }
    }

    internal sealed class BlackoutStrategy : ISpecialCardEffectStrategy
    {
        public SpecialCardId Id => SpecialCardId.Blackout;

        public bool CanResolve(ISpecialCardEffectHost host)
        {
            return true;
        }

        public void Accept(ISpecialCardEffectHost host)
        {
            host.ConcealCardsForCurrentHand();
            host.ApplyPressure(
                PressureChangeCause.Sc03BlackoutResolved,
                host.CurrentSpecialPressureDelta);
        }

        public void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid)
        {
        }
    }

    internal sealed class ThirdEyeStrategy : ISpecialCardEffectStrategy
    {
        public SpecialCardId Id => SpecialCardId.ThirdEye;

        public bool CanResolve(ISpecialCardEffectHost host)
        {
            return host.RemainingDrawPileNumericCount >= 2;
        }

        public void Accept(ISpecialCardEffectHost host)
        {
            host.ApplyPressure(
                PressureChangeCause.Sc04ThirdEyeResolved,
                host.CurrentSpecialPressureDelta);
            host.BeginBlindNumericChoice();
        }

        public void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid)
        {
        }
    }

    internal sealed class WeHaveADealStrategy : ISpecialCardEffectStrategy
    {
        public SpecialCardId Id => SpecialCardId.WeHaveADeal;

        public bool CanResolve(ISpecialCardEffectHost host)
        {
            return !host.EyeSurrendered;
        }

        public void Accept(ISpecialCardEffectHost host)
        {
            Money liquidWealth = host.AvailableMoney + host.ProtectedMoney + host.LockedBetAmount;
            host.CreditAvailable(liquidWealth);
            host.MarkEyeSurrendered();
            host.ReduceLucidityMaximumForSurrenderedEye();
            host.ApplyDealerRelationship(DealerRelationshipChangeCause.Sc05DealerDealAccepted);
            host.ApplyPressure(
                PressureChangeCause.Sc05DealerDealAccepted,
                host.CurrentSpecialPressureDelta);
        }

        public void Reject(ISpecialCardEffectHost host, bool rejectionCostPaid)
        {
            host.ApplyDealerRelationship(DealerRelationshipChangeCause.Sc05DealerDealRejected);
            if (!rejectionCostPaid)
            {
                host.ApplyPressure(
                    PressureChangeCause.Sc05DealerDealRejectedWithoutFunds,
                    host.DealRejectionPressureWhenUnfunded);
            }
        }
    }
}
