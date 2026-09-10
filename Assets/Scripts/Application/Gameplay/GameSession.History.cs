using System;
using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Diagnostics;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Application.Gameplay
{
    public sealed partial class GameSession
    {
        public IReadOnlyList<InternalRunHistoryEntry> InternalHistory =>
            _readOnlyInternalHistory ?? Array.Empty<InternalRunHistoryEntry>();

        private InternalResourceState CaptureInternalResources()
        {
            return new InternalResourceState(
                AvailableMoney,
                ProtectedMoney,
                LockedBetAmount,
                _psychology?.CreateSnapshot());
        }

        private void AppendInternalHistory(
            InternalHistoryKind kind,
            string technicalCode,
            string checkpoint,
            InternalResourceState resourcesBefore = null,
            RandomStreamState? streamBefore = null,
            RandomStreamState? streamAfter = null,
            int? roll = null,
            int? threshold = null,
            bool? succeeded = null,
            ContentDecisionOption? choice = null,
            Money costPaid = default,
            IReadOnlyList<CardId> affectedCards = null,
            IReadOnlyList<int> affectedValues = null,
            bool? emittedAnswer = null,
            bool? informationWasTruthful = null,
            bool? suspicionSignal = null,
            string effectExpiration = null)
        {
            if (!_phaseThreeEnabled)
            {
                return;
            }

            _internalHistory.Add(new InternalRunHistoryEntry(
                _internalHistory.Count + 1L,
                kind,
                technicalCode,
                checkpoint,
                CurrentCycleNumber,
                CurrentRoundNumber,
                CurrentHandNumber,
                streamBefore,
                streamAfter,
                roll,
                threshold,
                succeeded,
                choice,
                costPaid,
                resourcesBefore ?? CaptureInternalResources(),
                CaptureInternalResources(),
                affectedCards ?? Array.Empty<CardId>(),
                affectedValues ?? Array.Empty<int>(),
                emittedAnswer,
                informationWasTruthful,
                suspicionSignal,
                effectExpiration));
        }

        private void AppendRandomCheck(
            string technicalCode,
            string checkpoint,
            IRandomStream stream,
            RandomStreamState before,
            int roll,
            int threshold,
            bool succeeded)
        {
            AppendInternalHistory(
                InternalHistoryKind.RandomCheck,
                technicalCode,
                checkpoint,
                streamBefore: before,
                streamAfter: stream.CaptureState(),
                roll: roll,
                threshold: threshold,
                succeeded: succeeded);
        }
    }
}
