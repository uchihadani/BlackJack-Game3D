using System;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Gameplay;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;
using TwentyThree.Domain.Progression;
using TwentyThree.Application.Gameplay.Diagnostics;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        private bool EvaluateContentPlayerAfterCard(bool isInitialDeal)
        {
            HandScore playerScore = _handEvaluator.Evaluate(_playerHand);
            if (playerScore.Total == 24 &&
                !_it05CheckedForCurrentDraw &&
                HasUsableAutomaticItem(ItemId.IT05))
            {
                _it05CheckedForCurrentDraw = true;
                RandomStreamKey key = RandomStreamKeys.It05
                    .Derive(CurrentHandNumber)
                    .Derive(_it05CheckCount++);
                IRandomStream stream = _randomStreamFactory.Create(CurrentRoundSeed, key);
                RandomStreamState stateBefore = stream.CaptureState();
                int roll = stream.NextInt(BasisPoints.Scale);
                bool luckyCoinSucceeded =
                    roll < Rules.PhaseThree.LuckyCoinProbability.Value;
                AppendRandomCheck(
                    ItemId.IT05.ToTechnicalCode(),
                    "PostCardEvaluation",
                    stream,
                    stateBefore,
                    roll,
                    Rules.PhaseThree.LuckyCoinProbability.Value,
                    luckyCoinSucceeded);
                if (luckyCoinSucceeded)
                {
                    InternalResourceState resourcesBefore = CaptureInternalResources();
                    _playerHand.SetTotalModifier(-1);
                    InventoryOperationResult consumed =
                        _inventory.TryConsumeTableCharge(ItemId.IT05);
                    if (!consumed.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "The validated IT-05 charge could not be consumed.");
                    }

                    AppendInternalHistory(
                        InternalHistoryKind.ContentResolved,
                        ItemId.IT05.ToTechnicalCode(),
                        "PostCardEvaluation",
                        resourcesBefore,
                        succeeded: true,
                        effectExpiration: "EndOfHand");

                    ResolveHand();
                    return true;
                }
            }

            playerScore = _handEvaluator.Evaluate(_playerHand);
            if (playerScore.IsBust && HasUsableAutomaticItem(ItemId.IT02))
            {
                InternalResourceState resourcesBefore = CaptureInternalResources();
                InventoryOperationResult consumed =
                    _inventory.TryConsumeTableCharge(ItemId.IT02);
                if (!consumed.Succeeded)
                {
                    throw new InvalidOperationException(
                        "The validated IT-02 charge could not be consumed.");
                }

                _lastBreathPriceDoubled = true;
                AppendInternalHistory(
                    InternalHistoryKind.ContentResolved,
                    ItemId.IT02.ToTechnicalCode(),
                    "PostCardEvaluation",
                    resourcesBefore,
                    succeeded: true,
                    effectExpiration: "Immediate");
                PrepareContentResolutionVisibility();
                SetPhase(GamePhase.Resolution);
                HandResolution protectedResolution = _outcomeResolver.ProtectedDraw(
                    playerScore,
                    _handEvaluator.Evaluate(_dealerHand));
                CompleteHand(protectedResolution, false);
                return true;
            }

            if (playerScore.IsBust || playerScore.IsTwentyThree)
            {
                ResolveHand();
                return true;
            }

            return false;
        }

        private void CompleteContentHand(HandResolution resolution, bool closeRound)
        {
            BetOutcome betOutcome = ConvertOutcome(resolution.Outcome);
            BasisPoints bonus = _distractedBonusActive
                ? Rules.PhaseThree.Events.DistractedNetGainBonus
                : BasisPoints.Zero;
            bool wasAllIn = _lockedBet.IsAllIn;
            if (!_payoutCalculator.TrySettle(
                    _lockedBet,
                    betOutcome,
                    Rules.Payouts,
                    bonus,
                    _wallet,
                    out _))
            {
                throw new InvalidOperationException("The locked bet was already settled.");
            }

            LastResolution = resolution;
            _previousHandOutcome = resolution.Outcome;

            if (resolution.Outcome == HandOutcome.Loss && wasAllIn)
            {
                ApplyPressureChange(new PressureDelta(
                    PressureChangeCause.AllInLost,
                    Rules.PhaseThree.AllInLossPressure));
            }

            if (!_deck.CompleteHand())
            {
                throw new InvalidOperationException(
                    "The round deck could not complete the active hand.");
            }

            _progression.RecordCompletedHand();
            if (closeRound && _progression.Status == RunStatus.Active)
            {
                _progression.RequestRoundClosure();
            }

            _blackoutActive = false;
            _distractedActive = false;
            _distractedBonusActive = false;
            _blurredVisionCurrentHand = false;
            _activeDistortions.Clear();
            _pendingNumericDraw = false;
            _suspendedFlow = SuspendedGameplayFlow.None;
            SetPhase(_progression.Status == RunStatus.RoundClosurePending
                ? GamePhase.RoundSettlement
                : GamePhase.PostHand);
            _observerDispatcher.Publish(HandResolved, resolution);
        }

        private void PrepareContentResolutionVisibility()
        {
            _blackoutActive = false;
            _distractedActive = false;
            _activeDistortions.Clear();
            if (_deferredPlayerThirdCard.HasValue)
            {
                PublishDeferredThirdPlayerCard(false);
            }
        }

        private void CompleteContentRoundTransition()
        {
            ArchiveRound();
            _itemsBlockedForRound = false;

            if (_progression.Status == RunStatus.DebtDeadlinePending)
            {
                SetPhase(GamePhase.BeforeRunFailureCommitted);
                Money usableMoney = _wallet.Available + _wallet.Protected;
                if (usableMoney >= Rules.MinimumBet &&
                    HasUsableAutomaticItem(ItemId.IT03))
                {
                    InternalResourceState resourcesBefore = CaptureInternalResources();
                    InventoryOperationResult consumed =
                        _inventory.TryConsumeTableCharge(ItemId.IT03);
                    if (!consumed.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "The validated IT-03 charge could not be consumed.");
                    }

                    _progression.StartExtraordinaryRound();
                    LucidityChanged lucidity = _psychology.SetLucidity(
                        new LuciditySetRequest(
                            LuciditySetCause.It03SecondChanceActivated,
                            Rules.PhaseThree.SecondChanceLucidity));
                    _observerDispatcher.Publish(LucidityChanged, lucidity);
                    ApplyRelationshipChange(DealerRelationshipChange.DefaultFor(
                        DealerRelationshipChangeCause.It03SecondChanceActivated));
                    AppendInternalHistory(
                        InternalHistoryKind.ContentResolved,
                        ItemId.IT03.ToTechnicalCode(),
                        "BeforeRunFailureCommitted",
                        resourcesBefore,
                        succeeded: true,
                        effectExpiration: "EndOfExtraordinaryRound");
                    CreateRoundDeck();
                    ResetHands();
                    EnterBettingOrFundingRequired();
                    return;
                }

                _progression.CommitDebtDeadlineMissed();
            }

            if (_progression.Status == RunStatus.DebtDeadlineMissed)
            {
                CompleteRun(GamePhase.DebtDeadlineMissed);
                return;
            }

            CreateRoundDeck();
            ResetHands();
            EnterBettingOrFundingRequired();
        }
    }
}
