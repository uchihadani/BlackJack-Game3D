using TwentyThree.Application.Gameplay.Content;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        bool ISpecialCardEffectHost.EyeSurrendered => _eyeSurrendered;

        Money ISpecialCardEffectHost.AvailableMoney => _wallet.Available;

        Money ISpecialCardEffectHost.ProtectedMoney => _wallet.Protected;

        Money ISpecialCardEffectHost.LockedBetAmount => LockedBetAmount;

        int ISpecialCardEffectHost.RemainingDrawPileCount => _deck.DrawCount;

        int ISpecialCardEffectHost.RemainingDrawPileNumericCount =>
            _deck.RemainingNumericCount;

        int ISpecialCardEffectHost.CurrentSpecialPressureDelta =>
            _pendingSpecial.AppliedPressureDelta;

        int ISpecialCardEffectHost.DealRejectionPressureWhenUnfunded =>
            Rules.PhaseThree.DealRejectionPressureWhenUnfunded;

        void ISpecialCardEffectHost.ReshuffleRemainingDrawPile()
        {
            IRandomStream stream = GetRoundStream(RandomStreamKeys.Sc01Reshuffle);
            RandomStreamState before = stream.CaptureState();
            int derivedSeed = stream.NextInt(int.MaxValue);
            bool reshuffled = _deck.ReshuffleRemaining(derivedSeed);
            AppendRandomCheck(
                "SC-01/RESHUFFLE",
                "RemainingDrawPile",
                stream,
                before,
                derivedSeed,
                int.MaxValue,
                reshuffled);
        }

        void ISpecialCardEffectHost.BlockItemsForCurrentRound()
        {
            _itemsBlockedForRound = true;
        }

        void ISpecialCardEffectHost.ConcealCardsForCurrentHand()
        {
            _blackoutActive = true;
            _activeDistortions.Clear();
        }

        void ISpecialCardEffectHost.BeginBlindNumericChoice()
        {
            _awaitingBlindNumericChoice = true;
        }

        void ISpecialCardEffectHost.CreditAvailable(Money amount)
        {
            _wallet.CreditAvailable(amount);
        }

        void ISpecialCardEffectHost.MarkEyeSurrendered()
        {
            _eyeSurrendered = true;
        }

        void ISpecialCardEffectHost.ReduceLucidityMaximumForSurrenderedEye()
        {
            LucidityMaximumChangeResult result = _psychology.ReduceLucidityMaximum(
                new LucidityMaximumReduction(
                    LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                    Rules.PhaseThree.SurrenderedEyeLucidityMaximum));
            _observerDispatcher.Publish(LucidityMaximumChanged, result.MaximumChange);
            if (result.DidClampLucidity)
            {
                _observerDispatcher.Publish(LucidityChanged, result.LucidityChange);
            }
        }

        void ISpecialCardEffectHost.ApplyPressure(PressureChangeCause cause, int points)
        {
            ApplyPressureChange(new PressureDelta(cause, points));
        }

        void ISpecialCardEffectHost.ApplyDealerRelationship(
            DealerRelationshipChangeCause cause)
        {
            ApplyRelationshipChange(DealerRelationshipChange.DefaultFor(cause));
        }

        private void ApplyPressureChange(PressureDelta delta)
        {
            PressureChangeResult result = _psychology.ApplyPressure(delta);
            _observerDispatcher.Publish(PressureChanged, result.PressureChange);
            if (result.DidTriggerRupture)
            {
                _observerDispatcher.Publish(LucidityChanged, result.RuptureLucidityChange);
            }
        }

        private void ApplyLucidityChange(LucidityDelta delta)
        {
            LucidityChanged change = _psychology.AdjustLucidity(delta);
            _observerDispatcher.Publish(LucidityChanged, change);
        }

        private void ApplyRelationshipChange(DealerRelationshipChange change)
        {
            DealerRelationshipChangeResult result =
                _psychology.ApplyDealerRelationship(change);
            _observerDispatcher.Publish(
                DealerRelationshipChanged,
                result.PrimaryChange);
            if (result.SecondaryChange != null)
            {
                _observerDispatcher.Publish(
                    DealerRelationshipChanged,
                    result.SecondaryChange);
            }
        }
    }
}
