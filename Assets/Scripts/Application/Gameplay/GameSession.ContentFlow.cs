using System;
using System.Collections.Generic;
using System.Linq;
using TwentyThree.Application.Gameplay.Content;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        public GameCommandResult TryResolveContentDecision(ContentDecisionOption option)
        {
            if (IsTerminal())
            {
                return GameCommandResult.Reject(GameCommandFailure.RunFinished);
            }

            if (_actionInProgress)
            {
                return GameCommandResult.Reject(GameCommandFailure.ActionInProgress);
            }

            if (Phase == GamePhase.SpecialCardChoice)
            {
                return ResolveBlindNumericChoice(option);
            }

            if (Phase == GamePhase.ItemDecision)
            {
                if (option != ContentDecisionOption.Confirm &&
                    option != ContentDecisionOption.Cancel)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.InvalidContentChoice);
                }

                _actionInProgress = true;
                try
                {
                    return ResolveManualItemDecision(option);
                }
                finally
                {
                    _actionInProgress = false;
                }
            }

            if (Phase != GamePhase.SpecialCardDecision &&
                Phase != GamePhase.EventDecision)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.ContentDecisionUnavailable);
            }

            if (option != ContentDecisionOption.Accept &&
                option != ContentDecisionOption.Reject)
            {
                return GameCommandResult.Reject(GameCommandFailure.InvalidContentChoice);
            }

            _actionInProgress = true;
            try
            {
                return Phase == GamePhase.SpecialCardDecision
                    ? ResolveSpecialDecision(option)
                    : ResolveEventDecision(option);
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        private GameCommandResult BeginContentInitialDeal()
        {
            if (!_deck.TryStartHand())
            {
                throw new InvalidOperationException(
                    "The validated round deck could not start a hand.");
            }

            ResetContentHandState();
            _suspendedFlow = SuspendedGameplayFlow.InitialDeal;
            _initialDealSlot = 0;
            SetPhase(GamePhase.InitialDeal);
            ResumeContentFlow();
            return GameCommandResult.Success();
        }

        private GameCommandResult ExecuteContentHit()
        {
            ExpireCommandBoundDistortions();
            PressureChangeCause cause = _previousHandOutcome.HasValue &&
                                        _previousHandOutcome.Value == TwentyThree.Domain.Gameplay.HandOutcome.Loss
                ? PressureChangeCause.VoluntaryHitAfterPreviousLoss
                : PressureChangeCause.VoluntaryHit;
            int points = cause == PressureChangeCause.VoluntaryHitAfterPreviousLoss
                ? Rules.PhaseThree.VoluntaryHitAfterLossPressure
                : Rules.PhaseThree.VoluntaryHitPressure;
            ApplyPressureChange(new PressureDelta(cause, points));

            _suspendedFlow = SuspendedGameplayFlow.PlayerHit;
            SetPhase(GamePhase.DrawingPlayerCard);
            BeginPendingNumericDraw(CardRecipient.Player, false);
            ResumeContentFlow();
            return GameCommandResult.Success();
        }

        private GameCommandResult ExecuteContentStand()
        {
            ExpireCommandBoundDistortions();
            _suspendedFlow = SuspendedGameplayFlow.DealerTurn;
            SetPhase(GamePhase.DealerTurn);
            ResumeContentFlow();
            return GameCommandResult.Success();
        }

        private void ResumeContentFlow()
        {
            if (_pendingSpecial != null || _pendingEvent != null ||
                _awaitingBlindNumericChoice || IsTerminal())
            {
                return;
            }

            switch (_suspendedFlow)
            {
                case SuspendedGameplayFlow.InitialDeal:
                    ResumeInitialDeal();
                    break;
                case SuspendedGameplayFlow.PlayerHit:
                    ResumePlayerHit();
                    break;
                case SuspendedGameplayFlow.DealerTurn:
                    ResumeContentDealerTurn();
                    break;
                case SuspendedGameplayFlow.EnterPlayerTurn:
                    _suspendedFlow = SuspendedGameplayFlow.None;
                    SetPhase(GamePhase.PlayerTurn);
                    break;
            }
        }

        private void ResumeInitialDeal()
        {
            while (_initialDealSlot < 6)
            {
                int slot = _initialDealSlot;
                CardRecipient recipient = slot % 2 == 0
                    ? CardRecipient.Player
                    : CardRecipient.Dealer;
                bool faceDown = slot == 5;

                if (!_pendingNumericDraw)
                {
                    BeginPendingNumericDraw(recipient, faceDown);
                }

                if (!TryCompletePendingNumericDraw())
                {
                    return;
                }

                if (!AdvanceInitialDealAfterNumericCard(slot))
                {
                    return;
                }
            }

            SetPhase(GamePhase.PostCardEvaluation);
            if (EvaluateContentPlayerAfterCard(true))
            {
                return;
            }

            _suspendedFlow = SuspendedGameplayFlow.EnterPlayerTurn;
            CheckAndResolveEvent(GameEventId.VoicesFromBeyond);
            if (_pendingEvent == null)
            {
                ResumeContentFlow();
            }
        }

        private void ResumePlayerHit()
        {
            if (!TryCompletePendingNumericDraw())
            {
                return;
            }

            ContinuePlayerAfterNumericCard();
        }

        private void ContinuePlayerAfterNumericCard()
        {
            _suspendedFlow = SuspendedGameplayFlow.None;
            SetPhase(GamePhase.PostCardEvaluation);
            if (!EvaluateContentPlayerAfterCard(false))
            {
                SetPhase(GamePhase.PlayerTurn);
            }
        }

        private void ResumeContentDealerTurn()
        {
            if (!DealerHoleCardRevealed)
            {
                RevealDealerHoleCardWithContent();
            }

            while (true)
            {
                if (_pendingNumericDraw)
                {
                    if (!TryCompletePendingNumericDraw())
                    {
                        return;
                    }
                }

                HandScore dealerScore = _handEvaluator.Evaluate(_dealerHand);
                TwentyThree.Domain.Dealer.DealerDecision decision = _dealerStrategy.Decide(dealerScore);
                if (decision != TwentyThree.Domain.Dealer.DealerDecision.Hit)
                {
                    _suspendedFlow = SuspendedGameplayFlow.None;
                    ResolveHand();
                    return;
                }

                BeginPendingNumericDraw(CardRecipient.Dealer, false);
            }
        }

        private void BeginPendingNumericDraw(CardRecipient recipient, bool faceDown)
        {
            _pendingNumericDraw = true;
            _pendingDrawRecipient = recipient;
            _pendingDrawFaceDown = faceDown;
            _it05CheckedForCurrentDraw = false;
        }

        private bool TryCompletePendingNumericDraw()
        {
            while (_pendingNumericDraw)
            {
                DeckDrawStatus status = _deck.DrawEntry(out DeckEntry entry);
                if (status != DeckDrawStatus.Drawn)
                {
                    _pendingNumericDraw = false;
                    _suspendedFlow = SuspendedGameplayFlow.None;
                    ResolveTechnicalDraw();
                    return false;
                }

                if (entry.IsNumeric)
                {
                    CompletePendingNumericCard(entry.NumericCard);
                    return true;
                }

                if (OpenSpecialDecision(entry.SpecialCardId))
                {
                    return false;
                }
            }

            return false;
        }

        private void CompletePendingNumericCard(NumericCard card)
        {
            Hand hand = _pendingDrawRecipient == CardRecipient.Player
                ? _playerHand
                : _dealerHand;
            hand.Add(card);

            bool isDeferredThirdPlayerCard =
                _suspendedFlow == SuspendedGameplayFlow.InitialDeal &&
                _initialDealSlot == 4;

            if (_pendingDrawRecipient == CardRecipient.Dealer && !_pendingDrawFaceDown)
            {
                _visibleDealerCards.Add(card);
            }

            if (isDeferredThirdPlayerCard)
            {
                _deferredPlayerThirdCard = card;
            }
            else
            {
                PublishCardWithPerception(
                    card,
                    _pendingDrawRecipient,
                    _pendingDrawFaceDown,
                    _blackoutActive);
            }

            _pendingNumericDraw = false;
        }

        private bool OpenSpecialDecision(SpecialCardId id)
        {
            SpecialCardDefinition definition = Rules.PhaseThree.SpecialCards.Get(id);
            ISpecialCardEffectStrategy strategy = _specialStrategyFactory.Create(id);
            AppendInternalHistory(
                InternalHistoryKind.ContentEncountered,
                id.ToTechnicalCode(),
                "DeckDraw");
            if (!strategy.CanResolve(this))
            {
                AppendInternalHistory(
                    InternalHistoryKind.ContentResolved,
                    id.ToTechnicalCode(),
                    "DeckDraw",
                    succeeded: false);
                return false;
            }

            _pendingSpecial = definition;
            bool canReject = id == SpecialCardId.WeHaveADeal ||
                             _wallet.Available >= definition.RejectionCost;
            bool descriptionHidden = _psychology.PressureProfile.AreSpecialDescriptionsHidden;
            _pendingContentDecision = new PendingContentDecision(
                ContentSourceKind.SpecialCard,
                id.ToTechnicalCode(),
                definition.DisplayName,
                true,
                canReject,
                definition.RejectionCost,
                descriptionHidden,
                false);
            SetPhase(GamePhase.SpecialCardDecision);
            _observerDispatcher.Publish(
                SpecialCardEncountered,
                new SpecialCardEncounteredEvent(
                    id,
                    _pendingDrawRecipient,
                    _deck.EntryDrawHistory.Count - 1,
                    descriptionHidden));
            _observerDispatcher.Publish(ContentDecisionOpened, _pendingContentDecision);
            return true;
        }

        private GameCommandResult ResolveSpecialDecision(ContentDecisionOption option)
        {
            if (_pendingSpecial == null)
            {
                return GameCommandResult.Reject(
                    GameCommandFailure.ContentDecisionUnavailable);
            }

            ISpecialCardEffectStrategy strategy =
                _specialStrategyFactory.Create(_pendingSpecial.Id);
            SpecialCardId resolvedId = _pendingSpecial.Id;
            InternalResourceState resourcesBefore = CaptureInternalResources();
            Money costPaid = Money.Zero;
            IReadOnlyList<NumericCard> blindCandidates =
                resolvedId == SpecialCardId.ThirdEye &&
                option == ContentDecisionOption.Accept
                    ? _deck.GetFirstNumericCandidates(2)
                    : Array.Empty<NumericCard>();
            if (option == ContentDecisionOption.Accept)
            {
                strategy.Accept(this);
            }
            else
            {
                bool paid = false;
                if (_wallet.Available >= _pendingSpecial.RejectionCost)
                {
                    paid = _wallet.TryDebitAvailable(_pendingSpecial.RejectionCost);
                    if (paid)
                    {
                        costPaid = _pendingSpecial.RejectionCost;
                    }
                }
                else if (_pendingSpecial.Id != SpecialCardId.WeHaveADeal)
                {
                    return GameCommandResult.Reject(
                        GameCommandFailure.ContentRejectionUnavailable);
                }

                strategy.Reject(this, paid);
            }

            AppendInternalHistory(
                InternalHistoryKind.ContentResolved,
                resolvedId.ToTechnicalCode(),
                "DeckDraw",
                resourcesBefore,
                choice: option,
                costPaid: costPaid,
                affectedCards: blindCandidates.Select(card => card.Id).ToArray(),
                affectedValues: blindCandidates
                    .Select(GetBaseMechanicalVisualValue)
                    .ToArray(),
                effectExpiration: GetSpecialExpiration(resolvedId));

            _pendingContentDecision = null;
            if (_awaitingBlindNumericChoice)
            {
                _pendingContentDecision = new PendingContentDecision(
                    ContentSourceKind.SpecialCard,
                    SpecialCardId.ThirdEye.ToTechnicalCode(),
                    _pendingSpecial.DisplayName,
                    false,
                    false,
                    Money.Zero,
                    false,
                    true);
                SetPhase(GamePhase.SpecialCardChoice);
                _observerDispatcher.Publish(ContentDecisionOpened, _pendingContentDecision);
                return GameCommandResult.Success();
            }

            _pendingSpecial = null;
            ResumeContentFlow();
            return GameCommandResult.Success();
        }

        private GameCommandResult ResolveBlindNumericChoice(ContentDecisionOption option)
        {
            if (!_awaitingBlindNumericChoice ||
                (option != ContentDecisionOption.ChoiceA &&
                 option != ContentDecisionOption.ChoiceB))
            {
                return GameCommandResult.Reject(GameCommandFailure.InvalidContentChoice);
            }

            _actionInProgress = true;
            try
            {
                int candidateIndex = option == ContentDecisionOption.ChoiceA ? 0 : 1;
                IReadOnlyList<NumericCard> candidates =
                    _deck.GetFirstNumericCandidates(2);
                if (!_deck.TryDrawNumericCandidate(candidateIndex, out NumericCard card))
                {
                    throw new InvalidOperationException(
                        "The validated blind numeric candidate is no longer available.");
                }

                _awaitingBlindNumericChoice = false;
                _pendingContentDecision = null;
                _pendingSpecial = null;
                AppendInternalHistory(
                    InternalHistoryKind.BlindCardChoice,
                    SpecialCardId.ThirdEye.ToTechnicalCode(),
                    "PendingNumericReplacement",
                    choice: option,
                    affectedCards: candidates.Select(candidate => candidate.Id).ToArray(),
                    affectedValues: new[] { GetBaseMechanicalVisualValue(card) });
                CompletePendingNumericCard(card);
                ContinueAfterBlindNumericChoice();
                return GameCommandResult.Success();
            }
            finally
            {
                _actionInProgress = false;
            }
        }

        private void ContinueAfterBlindNumericChoice()
        {
            if (_suspendedFlow == SuspendedGameplayFlow.InitialDeal)
            {
                int slot = _initialDealSlot;
                if (!AdvanceInitialDealAfterNumericCard(slot))
                {
                    return;
                }

                ResumeContentFlow();
                return;
            }

            if (_suspendedFlow == SuspendedGameplayFlow.PlayerHit)
            {
                ContinuePlayerAfterNumericCard();
                return;
            }

            ResumeContentFlow();
        }

        private bool AdvanceInitialDealAfterNumericCard(int slot)
        {
            _initialDealSlot++;
            if (slot == 3)
            {
                CheckAndResolveEvent(GameEventId.Meow);
                if (_pendingEvent != null)
                {
                    return false;
                }
            }

            if (slot == 4)
            {
                CheckAndResolveEvent(GameEventId.Distracted);
                if (_pendingEvent != null)
                {
                    return false;
                }

                PublishDeferredThirdPlayerCard(false);
            }

            return true;
        }

        private void PublishDeferredThirdPlayerCard(bool hidden)
        {
            if (!_deferredPlayerThirdCard.HasValue)
            {
                return;
            }

            PublishCardWithPerception(
                _deferredPlayerThirdCard.Value,
                CardRecipient.Player,
                hidden,
                hidden || _blackoutActive);
            _deferredPlayerThirdCard = null;
        }

        private void RevealDealerHoleCardWithContent()
        {
            DealerHoleCardRevealed = true;
            NumericCard holeCard = _dealerHand.Cards[2];
            if (!_visibleDealerCards.Contains(holeCard))
            {
                _visibleDealerCards.Add(holeCard);
            }

            PublishCardPerception(
                holeCard,
                _blackoutActive
                    ? CardPerceptionVisibility.ExplicitlyHidden
                    : CardPerceptionVisibility.Visible);
            _observerDispatcher.Publish(DealerHoleCardRevealedEvent, holeCard);
        }

        private void PublishCardWithPerception(
            NumericCard card,
            CardRecipient recipient,
            bool faceDown,
            bool explicitlyHidden)
        {
            CardPerceptionVisibility visibility = faceDown
                ? CardPerceptionVisibility.FaceDown
                : explicitlyHidden
                    ? CardPerceptionVisibility.ExplicitlyHidden
                    : CardPerceptionVisibility.Visible;
            PublishCardPerception(card, visibility);
            _observerDispatcher.Publish(
                CardDrawn,
                new CardDrawnEvent(card, recipient, faceDown || explicitlyHidden));
        }

        private void PublishCardPerception(
            NumericCard card,
            CardPerceptionVisibility visibility)
        {
            _playerHand.TryGetValueOverride(card.Id, out int playerOverride);
            _dealerHand.TryGetValueOverride(card.Id, out int dealerOverride);
            int? mechanicalOverride = playerOverride > 0
                ? playerOverride
                : dealerOverride > 0
                    ? dealerOverride
                    : (int?)null;
            bool alreadyEvaluated = _perceptionEvaluatedCards.Contains(card.Id);
            CardPerceptionRecord record = _perceptionService.Evaluate(
                new CardPerceptionRequest(
                    card,
                    visibility,
                    alreadyEvaluated,
                    Pressure,
                    Lucidity,
                    mechanicalOverride),
                GetRoundStream(TwentyThree.Domain.Randomness.RandomStreamKeys.CardPerception));

            if (record.WasEvaluated)
            {
                _perceptionEvaluatedCards.Add(card.Id);
            }

            if (record.IsDistorted)
            {
                _activeDistortions[card.Id] = record;
            }

            AppendInternalHistory(
                InternalHistoryKind.PerceptionEvaluated,
                "CARD_PERCEPTION",
                visibility.ToString(),
                streamBefore: record.StreamStateBefore,
                streamAfter: record.StreamStateAfter,
                roll: record.Roll,
                threshold: record.WasEvaluated
                    ? record.Request.PressureProfile.CardDistortionProbability.Value
                    : (int?)null,
                succeeded: record.IsDistorted,
                affectedCards: new[] { card.Id },
                affectedValues: record.FalseVisualValue.HasValue
                    ? new[] { record.FalseVisualValue.Value }
                    : Array.Empty<int>(),
                effectExpiration: record.Expiration?.ToString());

            _observerDispatcher.Publish(CardPerceptionEvaluated, record);
        }

        private void ExpireCommandBoundDistortions()
        {
            if (_activeDistortions == null || _activeDistortions.Count == 0)
            {
                return;
            }

            CardId[] ids = new CardId[_activeDistortions.Count];
            _activeDistortions.Keys.CopyTo(ids, 0);
            foreach (CardId id in ids)
            {
                if (_activeDistortions[id].Expiration ==
                    DistortionExpiration.NextValidPlayerCommand)
                {
                    _activeDistortions.Remove(id);
                }
            }
        }

        private void ResetContentHandState()
        {
            _checkedEventsThisHand.Clear();
            _perceptionEvaluatedCards.Clear();
            _activeDistortions.Clear();
            _blackoutActive = false;
            _distractedActive = false;
            _distractedBonusActive = false;
            _blurredVisionCurrentHand = _blurredVisionNextHand;
            _blurredVisionNextHand = false;
            _voicesMessage = null;
            _pendingContentDecision = null;
            _pendingSpecial = null;
            _pendingEvent = null;
            _pendingManualItem = null;
            _awaitingBlindNumericChoice = false;
            _deferredPlayerThirdCard = null;
            _pendingNumericDraw = false;
            _it05CheckedForCurrentDraw = false;
        }

        private static string GetSpecialExpiration(SpecialCardId id)
        {
            return id switch
            {
                SpecialCardId.BeginnersLuck => "Immediate",
                SpecialCardId.PanicAttack => "EndOfRound",
                SpecialCardId.Blackout => "StartOfResolution",
                SpecialCardId.ThirdEye => "Immediate",
                SpecialCardId.WeHaveADeal => "EndOfRun",
                _ => throw new ArgumentOutOfRangeException(nameof(id))
            };
        }
    }
}
