using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Snapshots;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        public GameSessionSnapshot CreateSnapshot()
        {
            List<ItemStateSnapshot> items = new List<ItemStateSnapshot>();
            AddItemSnapshots(items, TableItems, InventoryLocation.Table);
            AddItemSnapshots(items, StoredItems, InventoryLocation.Storage);

            List<RandomStreamState> streams = new List<RandomStreamState>();
            if (_randomStreams != null)
            {
                foreach (IRandomStream stream in _randomStreams.Values)
                {
                    streams.Add(stream.CaptureState());
                }

                streams.Sort((left, right) =>
                    string.CompareOrdinal(left.Key.Value, right.Key.Value));
            }

            return new GameSessionSnapshot(
                RunSeed,
                CurrentRoundSeed,
                _roundSeedState,
                Phase,
                RunStatus,
                CurrentCycleNumber,
                CurrentRoundNumber,
                CurrentHandNumber,
                RuleRoundIndex,
                RoundOrdinal,
                RoundInstanceId,
                IsExtraordinaryRound,
                SecondChanceUsed,
                AvailableMoney,
                ProtectedMoney,
                RemainingDebt,
                LockedBetAmount,
                _lockedBet != null && _lockedBet.IsAllIn,
                LastBetAmount,
                _psychology?.CreateSnapshot(),
                _previousHandOutcome,
                CreateHandSnapshot(_playerHand),
                CreateHandSnapshot(_dealerHand),
                _visibleDealerCards,
                DealerHoleCardRevealed,
                items,
                CreateDeckSnapshot(),
                new ActiveEffectStateSnapshot(
                    _itemsBlockedForRound,
                    _blackoutActive,
                    _distractedActive,
                    _distractedBonusActive,
                    _blurredVisionCurrentHand,
                    _blurredVisionNextHand,
                    _eyeSurrendered,
                    _lastBreathPriceDoubled),
                new PendingFlowStateSnapshot(
                    _pendingContentDecision,
                    _pendingSpecial?.Id,
                    _pendingEvent?.Id,
                    _pendingManualItem,
                    _suspendedFlow.ToString(),
                    _pendingNumericDraw,
                    _pendingDrawRecipient,
                    _pendingDrawFaceDown,
                    _initialDealSlot,
                    _awaitingBlindNumericChoice,
                    _deferredPlayerThirdCard,
                    _phaseBeforeManualItemDecision),
                streams,
                _perceptionEvaluatedCards ?? (IEnumerable<CardId>)new CardId[0],
                ActiveDistortions,
                _activatedEventsThisRound ?? (IEnumerable<GameEventId>)new GameEventId[0],
                _checkedEventsThisHand ?? (IEnumerable<GameEventId>)new GameEventId[0],
                _it05CheckCount,
                _voicesMessage,
                _roundSeeds,
                _roundHistory,
                InternalHistory);
        }

        private PhaseThreeRunResultSnapshot CreatePhaseThreeRunResultSnapshot()
        {
            List<ItemStateSnapshot> items = new List<ItemStateSnapshot>();
            AddItemSnapshots(items, TableItems, InventoryLocation.Table);
            AddItemSnapshots(items, StoredItems, InventoryLocation.Storage);
            return new PhaseThreeRunResultSnapshot(
                _psychology.CreateSnapshot(),
                items,
                _eyeSurrendered,
                _lastBreathPriceDoubled,
                _progression.SecondChanceUsed,
                _progression.RuleRoundIndex,
                _progression.RoundOrdinal,
                _progression.RoundInstanceId,
                _progression.IsExtraordinary);
        }

        private static HandStateSnapshot CreateHandSnapshot(Hand hand)
        {
            List<CardValueOverrideSnapshot> overrides =
                new List<CardValueOverrideSnapshot>();
            foreach (NumericCard card in hand.Cards)
            {
                if (hand.TryGetValueOverride(card.Id, out int value))
                {
                    overrides.Add(new CardValueOverrideSnapshot(card.Id, value));
                }
            }

            return new HandStateSnapshot(hand.Cards, overrides, hand.TotalModifier);
        }

        private RoundDeckStateSnapshot CreateDeckSnapshot()
        {
            return new RoundDeckStateSnapshot(
                _deck.Seed,
                _deck.HandInProgress,
                _deck.DrawPileEntries,
                _deck.DiscardPile,
                _deck.ActiveCards,
                _deck.InitialEntryOrder,
                _deck.EntryDrawHistory,
                _deck.RetiredSpecialCards,
                _deck.ReshuffleHistory);
        }

        private static void AddItemSnapshots(
            ICollection<ItemStateSnapshot> destination,
            IReadOnlyList<ItemInstance> source,
            InventoryLocation location)
        {
            foreach (ItemInstance item in source)
            {
                destination.Add(new ItemStateSnapshot(
                    item.Id,
                    location,
                    item.ChargesRemaining));
            }
        }
    }
}
