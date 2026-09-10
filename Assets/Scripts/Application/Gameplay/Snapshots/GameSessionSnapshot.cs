using System;
using System.Collections.Generic;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;
using TwentyThree.Domain.SpecialCards;
using TwentyThree.Application.Gameplay.Diagnostics;

namespace TwentyThree.Application.Gameplay.Snapshots
{
    public sealed class ItemStateSnapshot
    {
        public ItemStateSnapshot(ItemId id, InventoryLocation location, int chargesRemaining)
        {
            Id = id;
            Location = location;
            ChargesRemaining = chargesRemaining;
        }

        public ItemId Id { get; }

        public InventoryLocation Location { get; }

        public int ChargesRemaining { get; }
    }

    public sealed class CardValueOverrideSnapshot
    {
        public CardValueOverrideSnapshot(CardId cardId, int value)
        {
            CardId = cardId;
            Value = value;
        }

        public CardId CardId { get; }

        public int Value { get; }
    }

    public sealed class HandStateSnapshot
    {
        public HandStateSnapshot(
            IReadOnlyList<NumericCard> cards,
            IReadOnlyList<CardValueOverrideSnapshot> valueOverrides,
            int totalModifier)
        {
            Cards = SnapshotCopy.List(cards);
            ValueOverrides = SnapshotCopy.List(valueOverrides);
            TotalModifier = totalModifier;
        }

        public IReadOnlyList<NumericCard> Cards { get; }

        public IReadOnlyList<CardValueOverrideSnapshot> ValueOverrides { get; }

        public int TotalModifier { get; }
    }

    public sealed class RoundDeckStateSnapshot
    {
        public RoundDeckStateSnapshot(
            int seed,
            bool handInProgress,
            IReadOnlyList<DeckEntry> drawPile,
            IReadOnlyList<NumericCard> discardPile,
            IReadOnlyList<NumericCard> activeCards,
            IReadOnlyList<DeckEntry> initialOrder,
            IReadOnlyList<DeckEntry> drawHistory,
            IReadOnlyList<SpecialCardId> retiredSpecialCards,
            IReadOnlyList<DeckReshuffleRecord> reshuffleHistory)
        {
            Seed = seed;
            HandInProgress = handInProgress;
            DrawPile = SnapshotCopy.List(drawPile);
            DiscardPile = SnapshotCopy.List(discardPile);
            ActiveCards = SnapshotCopy.List(activeCards);
            InitialOrder = SnapshotCopy.List(initialOrder);
            DrawHistory = SnapshotCopy.List(drawHistory);
            RetiredSpecialCards = SnapshotCopy.List(retiredSpecialCards);
            ReshuffleHistory = SnapshotCopy.List(reshuffleHistory);
        }

        public int Seed { get; }

        public bool HandInProgress { get; }

        public IReadOnlyList<DeckEntry> DrawPile { get; }

        public IReadOnlyList<NumericCard> DiscardPile { get; }

        public IReadOnlyList<NumericCard> ActiveCards { get; }

        public IReadOnlyList<DeckEntry> InitialOrder { get; }

        public IReadOnlyList<DeckEntry> DrawHistory { get; }

        public IReadOnlyList<SpecialCardId> RetiredSpecialCards { get; }

        public IReadOnlyList<DeckReshuffleRecord> ReshuffleHistory { get; }
    }

    public sealed class ActiveEffectStateSnapshot
    {
        public ActiveEffectStateSnapshot(
            bool itemsBlockedForRound,
            bool blackoutActive,
            bool distractedActive,
            bool distractedBonusActive,
            bool blurredVisionCurrentHand,
            bool blurredVisionNextHand,
            bool eyeSurrendered,
            bool lastBreathPriceDoubled)
        {
            ItemsBlockedForRound = itemsBlockedForRound;
            BlackoutActive = blackoutActive;
            DistractedActive = distractedActive;
            DistractedBonusActive = distractedBonusActive;
            BlurredVisionCurrentHand = blurredVisionCurrentHand;
            BlurredVisionNextHand = blurredVisionNextHand;
            EyeSurrendered = eyeSurrendered;
            LastBreathPriceDoubled = lastBreathPriceDoubled;
        }

        public bool ItemsBlockedForRound { get; }

        public bool BlackoutActive { get; }

        public bool DistractedActive { get; }

        public bool DistractedBonusActive { get; }

        public bool BlurredVisionCurrentHand { get; }

        public bool BlurredVisionNextHand { get; }

        public bool EyeSurrendered { get; }

        public bool LastBreathPriceDoubled { get; }
    }

    public sealed class PendingFlowStateSnapshot
    {
        public PendingFlowStateSnapshot(
            PendingContentDecision decision,
            SpecialCardId? specialCardId,
            GameEventId? gameEventId,
            ItemId? itemId,
            string suspendedFlow,
            bool numericDrawPending,
            CardRecipient drawRecipient,
            bool drawFaceDown,
            int initialDealSlot,
            bool blindChoicePending,
            NumericCard? deferredPlayerThirdCard,
            GamePhase phaseBeforeItemDecision)
        {
            Decision = decision;
            SpecialCardId = specialCardId;
            GameEventId = gameEventId;
            ItemId = itemId;
            SuspendedFlow = suspendedFlow ?? string.Empty;
            NumericDrawPending = numericDrawPending;
            DrawRecipient = drawRecipient;
            DrawFaceDown = drawFaceDown;
            InitialDealSlot = initialDealSlot;
            BlindChoicePending = blindChoicePending;
            DeferredPlayerThirdCard = deferredPlayerThirdCard;
            PhaseBeforeItemDecision = phaseBeforeItemDecision;
        }

        public PendingContentDecision Decision { get; }

        public SpecialCardId? SpecialCardId { get; }

        public GameEventId? GameEventId { get; }

        public ItemId? ItemId { get; }

        public string SuspendedFlow { get; }

        public bool NumericDrawPending { get; }

        public CardRecipient DrawRecipient { get; }

        public bool DrawFaceDown { get; }

        public int InitialDealSlot { get; }

        public bool BlindChoicePending { get; }

        public NumericCard? DeferredPlayerThirdCard { get; }

        public GamePhase PhaseBeforeItemDecision { get; }
    }

    public sealed class GameSessionSnapshot
    {
        public GameSessionSnapshot(
            int runSeed,
            int currentRoundSeed,
            int roundSeedState,
            GamePhase phase,
            RunStatus runStatus,
            int cycleNumber,
            int roundNumber,
            int handNumber,
            int ruleRoundIndex,
            int roundOrdinal,
            long roundInstanceId,
            bool isExtraordinaryRound,
            bool secondChanceUsed,
            Money availableMoney,
            Money protectedMoney,
            Money remainingDebt,
            Money lockedBet,
            bool lockedBetIsAllIn,
            Money lastBet,
            PsychologySnapshot psychology,
            HandOutcome? previousHandOutcome,
            HandStateSnapshot playerHand,
            HandStateSnapshot dealerHand,
            IReadOnlyList<NumericCard> visibleDealerCards,
            bool dealerHoleCardRevealed,
            IReadOnlyList<ItemStateSnapshot> items,
            RoundDeckStateSnapshot deck,
            ActiveEffectStateSnapshot activeEffects,
            PendingFlowStateSnapshot pendingFlow,
            IReadOnlyList<RandomStreamState> randomStreams,
            IEnumerable<CardId> perceptionEvaluatedCards,
            IEnumerable<CardPerceptionRecord> activeDistortions,
            IEnumerable<GameEventId> activatedEventsThisRound,
            IEnumerable<GameEventId> checkedEventsThisHand,
            int luckyCoinCheckCount,
            VoicesMessage? voicesMessage,
            IReadOnlyList<int> roundSeeds,
            IReadOnlyList<RoundHistoryEntry> completedRoundHistory,
            IReadOnlyList<InternalRunHistoryEntry> internalHistory)
        {
            RunSeed = runSeed;
            CurrentRoundSeed = currentRoundSeed;
            RoundSeedState = roundSeedState;
            Phase = phase;
            RunStatus = runStatus;
            CycleNumber = cycleNumber;
            RoundNumber = roundNumber;
            HandNumber = handNumber;
            RuleRoundIndex = ruleRoundIndex;
            RoundOrdinal = roundOrdinal;
            RoundInstanceId = roundInstanceId;
            IsExtraordinaryRound = isExtraordinaryRound;
            SecondChanceUsed = secondChanceUsed;
            AvailableMoney = availableMoney;
            ProtectedMoney = protectedMoney;
            RemainingDebt = remainingDebt;
            LockedBet = lockedBet;
            LockedBetIsAllIn = lockedBetIsAllIn;
            LastBet = lastBet;
            Psychology = psychology;
            PreviousHandOutcome = previousHandOutcome;
            PlayerHand = playerHand;
            DealerHand = dealerHand;
            VisibleDealerCards = SnapshotCopy.List(visibleDealerCards);
            DealerHoleCardRevealed = dealerHoleCardRevealed;
            Items = SnapshotCopy.List(items);
            Deck = deck;
            ActiveEffects = activeEffects;
            PendingFlow = pendingFlow;
            RandomStreams = SnapshotCopy.List(randomStreams);
            PerceptionEvaluatedCards = SnapshotCopy.List(perceptionEvaluatedCards);
            ActiveDistortions = SnapshotCopy.List(activeDistortions);
            ActivatedEventsThisRound = SnapshotCopy.List(activatedEventsThisRound);
            CheckedEventsThisHand = SnapshotCopy.List(checkedEventsThisHand);
            LuckyCoinCheckCount = luckyCoinCheckCount;
            VoicesMessage = voicesMessage;
            RoundSeeds = SnapshotCopy.List(roundSeeds);
            CompletedRoundHistory = SnapshotCopy.List(completedRoundHistory);
            InternalHistory = SnapshotCopy.List(internalHistory);
        }

        public int RunSeed { get; }
        public int CurrentRoundSeed { get; }
        public int RoundSeedState { get; }
        public GamePhase Phase { get; }
        public RunStatus RunStatus { get; }
        public int CycleNumber { get; }
        public int RoundNumber { get; }
        public int HandNumber { get; }
        public int RuleRoundIndex { get; }
        public int RoundOrdinal { get; }
        public long RoundInstanceId { get; }
        public bool IsExtraordinaryRound { get; }
        public bool SecondChanceUsed { get; }
        public Money AvailableMoney { get; }
        public Money ProtectedMoney { get; }
        public Money RemainingDebt { get; }
        public Money LockedBet { get; }
        public bool LockedBetIsAllIn { get; }
        public Money LastBet { get; }
        public PsychologySnapshot Psychology { get; }
        public HandOutcome? PreviousHandOutcome { get; }
        public HandStateSnapshot PlayerHand { get; }
        public HandStateSnapshot DealerHand { get; }
        public IReadOnlyList<NumericCard> VisibleDealerCards { get; }
        public bool DealerHoleCardRevealed { get; }
        public IReadOnlyList<ItemStateSnapshot> Items { get; }
        public RoundDeckStateSnapshot Deck { get; }
        public ActiveEffectStateSnapshot ActiveEffects { get; }
        public PendingFlowStateSnapshot PendingFlow { get; }
        public IReadOnlyList<RandomStreamState> RandomStreams { get; }
        public IReadOnlyList<CardId> PerceptionEvaluatedCards { get; }
        public IReadOnlyList<CardPerceptionRecord> ActiveDistortions { get; }
        public IReadOnlyList<GameEventId> ActivatedEventsThisRound { get; }
        public IReadOnlyList<GameEventId> CheckedEventsThisHand { get; }
        public int LuckyCoinCheckCount { get; }
        public VoicesMessage? VoicesMessage { get; }
        public IReadOnlyList<int> RoundSeeds { get; }
        public IReadOnlyList<RoundHistoryEntry> CompletedRoundHistory { get; }
        public IReadOnlyList<InternalRunHistoryEntry> InternalHistory { get; }
    }

    internal static class SnapshotCopy
    {
        public static IReadOnlyList<T> List<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            return Array.AsReadOnly(new List<T>(source).ToArray());
        }
    }
}
