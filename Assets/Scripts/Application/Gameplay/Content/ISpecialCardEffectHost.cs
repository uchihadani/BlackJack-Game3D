using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay.Content
{
    internal interface ISpecialCardEffectHost
    {
        bool EyeSurrendered { get; }

        Money AvailableMoney { get; }

        Money ProtectedMoney { get; }

        Money LockedBetAmount { get; }

        int RemainingDrawPileCount { get; }

        int RemainingDrawPileNumericCount { get; }

        int CurrentSpecialPressureDelta { get; }

        int DealRejectionPressureWhenUnfunded { get; }

        void ReshuffleRemainingDrawPile();

        void BlockItemsForCurrentRound();

        void ConcealCardsForCurrentHand();

        void BeginBlindNumericChoice();

        void CreditAvailable(Money amount);

        void MarkEyeSurrendered();

        void ReduceLucidityMaximumForSurrenderedEye();

        void ApplyPressure(PressureChangeCause cause, int points);

        void ApplyDealerRelationship(DealerRelationshipChangeCause cause);
    }
}
