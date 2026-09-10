using System;
using System.Collections.Generic;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public interface IGameSession
    {
        GameRules Rules { get; }

        GamePhase Phase { get; }

        RunStatus RunStatus { get; }

        int CurrentCycleNumber { get; }

        int CurrentRoundNumber { get; }

        int CurrentHandNumber { get; }

        int RuleRoundIndex { get; }

        int RoundOrdinal { get; }

        long RoundInstanceId { get; }

        bool IsExtraordinaryRound { get; }

        Money AvailableMoney { get; }

        Money ProtectedMoney { get; }

        Money RemainingDebt { get; }

        Money CurrentMaximumBet { get; }

        Money ProtectedFundsCapacity { get; }

        bool CanAffordMinimumBet { get; }

        bool HasLockedBet { get; }

        Money LockedBetAmount { get; }

        Money LastBetAmount { get; }

        bool DealerHoleCardRevealed { get; }

        int Pressure { get; }

        PressureBand PressureBand { get; }

        int Lucidity { get; }

        int LucidityMaximum { get; }

        DealerRelationship DealerRelationship { get; }

        IReadOnlyList<ItemInstance> TableItems { get; }

        IReadOnlyList<ItemInstance> StoredItems { get; }

        bool ItemsBlockedForCurrentRound { get; }

        bool BlackoutActive { get; }

        bool DistractedActive { get; }

        bool BlurredVisionActive { get; }

        bool EyeSurrendered { get; }

        bool LastBreathPriceDoubled { get; }

        bool SecondChanceUsed { get; }

        bool IsPreparationWindowOpen { get; }

        PendingContentDecision PendingContentDecision { get; }

        VoicesMessage? CurrentVoicesMessage { get; }

        GameSessionReadModel ReadModel { get; }

        IReadOnlyList<Exception> ObserverFailures { get; }

        event Action<GamePhase> PhaseChanged;

        event Action<PendingContentDecision> ContentDecisionOpened;

        event Action<VoicesMessage> VoicesMessageCreated;

        event Action<PressureChanged> PressureChanged;

        event Action<LucidityChanged> LucidityChanged;

        event Action<LucidityMaximumChanged> LucidityMaximumChanged;

        event Action<DealerRelationshipChanged> DealerRelationshipChanged;

        event Action<GameSessionReadModel> ReadModelChanged;

        GameCommandResult TryConfirmBet(Money amount);

        GameCommandResult TryConfirmAllIn();

        GameCommandResult TryCancelBet();

        GameCommandResult TryHit();

        GameCommandResult TryStand();

        GameCommandResult TryPayDebt(Money amount, bool useProtectedFunds = false);

        GameCommandResult TryPayDebt(DebtPaymentAllocation allocation);

        GameCommandResult TryResolveContentDecision(ContentDecisionOption option);

        GameCommandResult TryUseItem(ItemId itemId);

        GameCommandResult TryPurchaseItem(ItemId itemId, InventoryLocation destination);

        GameCommandResult TryMoveItem(ItemId itemId, InventoryLocation destination);

        GameCommandResult TrySwapItems(ItemId movingItemId, ItemId destinationItemId);

        GameCommandResult TryDiscardItem(ItemId itemId);

        GameCommandResult TryDepositProtected(Money amount);

        GameCommandResult TryWithdrawProtected(Money amount);

        GameCommandResult TryExpireTimedCardDistortion(CardId cardId);

        GameCommandResult TryContinue();

        GameCommandResult TryCloseRound();

        GameCommandResult TryAbandonRound();
    }
}
