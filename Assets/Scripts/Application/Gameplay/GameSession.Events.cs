using System;
using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Content;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        GameEventDefinition IGameEventEffectHost.CurrentEventDefinition => _pendingEvent;

        void IGameEventEffectHost.ResolveMeow()
        {
            ResolveMeow();
        }

        void IGameEventEffectHost.ResolveVoicesFromBeyond()
        {
            ResolveVoicesFromBeyond();
        }

        void IGameEventEffectHost.ActivateDistracted()
        {
            ActivateDistracted();
        }

        private void CheckAndResolveEvent(GameEventId id)
        {
            if (_activatedEventsThisRound.Contains(id) ||
                !_checkedEventsThisHand.Add(id))
            {
                return;
            }

            GameEventDefinition definition = Rules.PhaseThree.Events.Get(id);
            BasisPoints effectiveProbability = _psychology.PressureProfile
                .CalculateEffectiveEventProbability(definition.BaseProbability);
            IRandomStream triggerStream = GetRoundStream(GetTriggerStreamKey(id));
            RandomStreamState triggerStateBefore = triggerStream.CaptureState();
            int triggerRoll = triggerStream.NextInt(BasisPoints.Scale);
            AppendRandomCheck(
                id.ToTechnicalCode(),
                GetEventCheckpoint(id),
                triggerStream,
                triggerStateBefore,
                triggerRoll,
                effectiveProbability.Value,
                triggerRoll < effectiveProbability.Value);
            if (triggerRoll >= effectiveProbability.Value)
            {
                return;
            }

            _activatedEventsThisRound.Add(id);
            _pendingEvent = definition;
            AppendInternalHistory(
                InternalHistoryKind.ContentEncountered,
                id.ToTechnicalCode(),
                GetEventCheckpoint(id));
            _observerDispatcher.Publish(
                GameEventEncountered,
                new GameEventEncounteredEvent(
                    id,
                    triggerRoll,
                    effectiveProbability.Value));

            IGameEventEffectStrategy strategy = _eventStrategyFactory.Create(id);
            if (!strategy.RequiresDecision ||
                definition.CanReject && _wallet.Available < definition.RejectionCost)
            {
                InternalResourceState resourcesBefore = CaptureInternalResources();
                strategy.Accept(this);
                AppendInternalHistory(
                    InternalHistoryKind.ContentResolved,
                    id.ToTechnicalCode(),
                    GetEventCheckpoint(id),
                    resourcesBefore,
                    choice: ContentDecisionOption.Accept,
                    effectExpiration: GetEventExpiration(id));
                _pendingEvent = null;
                return;
            }

            _pendingContentDecision = new PendingContentDecision(
                ContentSourceKind.GameEvent,
                id.ToTechnicalCode(),
                definition.DisplayName,
                true,
                definition.CanReject,
                definition.RejectionCost,
                false,
                false);
            SetPhase(GamePhase.EventDecision);
            _observerDispatcher.Publish(ContentDecisionOpened, _pendingContentDecision);
        }

        private GameCommandResult ResolveEventDecision(ContentDecisionOption option)
        {
            if (_pendingEvent == null)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.ContentDecisionUnavailable);
            }

            IGameEventEffectStrategy strategy =
                _eventStrategyFactory.Create(_pendingEvent.Id);
            GameEventId resolvedId = _pendingEvent.Id;
            InternalResourceState resourcesBefore = CaptureInternalResources();
            Money costPaid = Money.Zero;
            if (option == ContentDecisionOption.Reject)
            {
                if (!_pendingEvent.CanReject ||
                    _wallet.Available < _pendingEvent.RejectionCost)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.ContentRejectionUnavailable);
                }

                if (!_wallet.TryDebitAvailable(_pendingEvent.RejectionCost))
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.ContentRejectionUnavailable);
                }

                costPaid = _pendingEvent.RejectionCost;

                strategy.Reject(this);
                if (resolvedId == GameEventId.Distracted)
                {
                    PublishDeferredThirdPlayerCard(false);
                }
            }
            else
            {
                strategy.Accept(this);
            }

            AppendInternalHistory(
                InternalHistoryKind.ContentResolved,
                resolvedId.ToTechnicalCode(),
                GetEventCheckpoint(resolvedId),
                resourcesBefore,
                choice: option,
                costPaid: costPaid,
                effectExpiration: GetEventExpiration(resolvedId));

            _pendingEvent = null;
            _pendingContentDecision = null;
            ResumeContentFlow();
            return GameCommandResult.Success();
        }

        private void ResolveMeow()
        {
            NumericCard[] candidates =
            {
                _playerHand.Cards[0],
                _dealerHand.Cards[0],
                _playerHand.Cards[1],
                _dealerHand.Cards[1]
            };
            IRandomStream targetStream = GetRoundStream(RandomStreamKeys.Ev02Target);
            RandomStreamState targetBefore = targetStream.CaptureState();
            int targetIndex = targetStream.NextInt(candidates.Length);
            AppendRandomCheck(
                "EV-02/TARGET",
                "AfterP1D1P2D2",
                targetStream,
                targetBefore,
                targetIndex,
                candidates.Length,
                true);
            NumericCard selected = candidates[targetIndex];
            int baseValue = GetBaseMechanicalVisualValue(selected);
            List<int> values = new List<int>(9);
            for (int value = 1; value <= 10; value++)
            {
                if (value != baseValue)
                {
                    values.Add(value);
                }
            }

            IRandomStream valueStream = GetRoundStream(RandomStreamKeys.Ev02Value);
            RandomStreamState valueBefore = valueStream.CaptureState();
            int overrideIndex = valueStream.NextInt(values.Count);
            AppendRandomCheck(
                "EV-02/VALUE",
                "AfterP1D1P2D2",
                valueStream,
                valueBefore,
                overrideIndex,
                values.Count,
                true);
            Hand targetHand = targetIndex % 2 == 0 ? _playerHand : _dealerHand;
            targetHand.TrySetValueOverride(selected.Id, values[overrideIndex]);
            _activeDistortions.Remove(selected.Id);
            _perceptionEvaluatedCards.Add(selected.Id);
            AppendInternalHistory(
                InternalHistoryKind.ContentResolved,
                "EV-02/OVERRIDE",
                "AfterP1D1P2D2",
                affectedCards: new[] { selected.Id },
                affectedValues: new[] { values[overrideIndex] },
                effectExpiration: "EndOfHand");
            ApplyLucidityChange(new LucidityDelta(
                LucidityAdjustmentCause.Ev02MiauResolved,
                _pendingEvent.LucidityDelta));
        }

        private void ResolveVoicesFromBeyond()
        {
            bool actualDealerTwentyThree = _handEvaluator.Evaluate(_dealerHand).IsTwentyThree;
            IRandomStream truthStream = GetRoundStream(RandomStreamKeys.Ev01Truth);
            RandomStreamState truthBefore = truthStream.CaptureState();
            int truthRoll = truthStream.NextInt(BasisPoints.Scale);
            bool truthful = truthRoll <
                             Rules.PhaseThree.Events.VoicesTruthProbability.Value;
            AppendRandomCheck(
                "EV-01/TRUTH",
                "BeforePlayerTurn",
                truthStream,
                truthBefore,
                truthRoll,
                Rules.PhaseThree.Events.VoicesTruthProbability.Value,
                truthful);
            bool emittedAnswer = truthful
                ? actualDealerTwentyThree
                : !actualDealerTwentyThree;
            bool suspicionSignal = false;
            if (Lucidity >= 70)
            {
                BasisPoints threshold = truthful
                    ? Rules.PhaseThree.Events.TrueMessageSignalProbability
                    : Rules.PhaseThree.Events.FalseMessageSignalProbability;
                IRandomStream signalStream = GetRoundStream(RandomStreamKeys.Ev01Signal);
                RandomStreamState signalBefore = signalStream.CaptureState();
                int signalRoll = signalStream.NextInt(BasisPoints.Scale);
                suspicionSignal = signalRoll < threshold.Value;
                AppendRandomCheck(
                    "EV-01/SIGNAL",
                    "BeforePlayerTurn",
                    signalStream,
                    signalBefore,
                    signalRoll,
                    threshold.Value,
                    suspicionSignal);
            }

            _voicesMessage = new VoicesMessage(emittedAnswer, suspicionSignal);
            AppendInternalHistory(
                InternalHistoryKind.ContentResolved,
                "EV-01/MESSAGE",
                "BeforePlayerTurn",
                emittedAnswer: emittedAnswer,
                informationWasTruthful: truthful,
                suspicionSignal: suspicionSignal,
                effectExpiration: "EndOfHand");
            _observerDispatcher.Publish(VoicesMessageCreated, _voicesMessage.Value);
            ApplyPressureChange(new PressureDelta(
                PressureChangeCause.Ev01VoicesBeyondResolved,
                _pendingEvent.PressureDelta));
        }

        private void ActivateDistracted()
        {
            _distractedActive = true;
            _distractedBonusActive = true;
            ApplyLucidityChange(new LucidityDelta(
                LucidityAdjustmentCause.Ev03DistractedResolved,
                _pendingEvent.LucidityDelta));
            PublishDeferredThirdPlayerCard(true);
        }

        private static RandomStreamKey GetTriggerStreamKey(GameEventId id)
        {
            return id switch
            {
                GameEventId.VoicesFromBeyond => RandomStreamKeys.Ev01Trigger,
                GameEventId.Meow => RandomStreamKeys.Ev02Trigger,
                GameEventId.Distracted => RandomStreamKeys.Ev03Trigger,
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };
        }

        private static string GetEventCheckpoint(GameEventId id)
        {
            return id switch
            {
                GameEventId.VoicesFromBeyond => "BeforePlayerTurn",
                GameEventId.Meow => "AfterP1D1P2D2",
                GameEventId.Distracted => "BeforeShowingP3",
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };
        }

        private static string GetEventExpiration(GameEventId id)
        {
            return id switch
            {
                GameEventId.VoicesFromBeyond => "EndOfHand",
                GameEventId.Meow => "EndOfHand",
                GameEventId.Distracted => "StartOfResolution",
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };
        }

        private static int GetBaseMechanicalVisualValue(NumericCard card)
        {
            if (card.Rank == CardRank.Ace)
            {
                return 1;
            }

            return card.Rank >= CardRank.Ten ? 10 : (int)card.Rank;
        }
    }
}
