using System.Collections.Generic;
using TwentyThree.Application.Gameplay.Snapshots;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Application.Gameplay.Diagnostics
{
    public enum InternalHistoryKind
    {
        ContentEncountered = 0,
        RandomCheck = 1,
        ContentResolved = 2,
        BlindCardChoice = 3,
        ItemPreview = 4,
        ItemResolved = 5,
        PerceptionEvaluated = 6
    }

    public sealed class InternalResourceState
    {
        public InternalResourceState(
            Money availableMoney,
            Money protectedMoney,
            Money lockedBet,
            PsychologySnapshot psychology)
        {
            AvailableMoney = availableMoney;
            ProtectedMoney = protectedMoney;
            LockedBet = lockedBet;
            Psychology = psychology;
        }

        public Money AvailableMoney { get; }

        public Money ProtectedMoney { get; }

        public Money LockedBet { get; }

        public PsychologySnapshot Psychology { get; }
    }

    public sealed class InternalRunHistoryEntry
    {
        public InternalRunHistoryEntry(
            long sequence,
            InternalHistoryKind kind,
            string technicalCode,
            string checkpoint,
            int cycleNumber,
            int roundNumber,
            int handNumber,
            RandomStreamState? streamStateBefore,
            RandomStreamState? streamStateAfter,
            int? roll,
            int? threshold,
            bool? succeeded,
            ContentDecisionOption? choice,
            Money costPaid,
            InternalResourceState resourcesBefore,
            InternalResourceState resourcesAfter,
            IReadOnlyList<CardId> affectedCardIds,
            IReadOnlyList<int> affectedRealValues,
            bool? emittedAnswer,
            bool? informationWasTruthful,
            bool? suspicionSignal,
            string effectExpiration)
        {
            Sequence = sequence;
            Kind = kind;
            TechnicalCode = technicalCode ?? string.Empty;
            Checkpoint = checkpoint ?? string.Empty;
            CycleNumber = cycleNumber;
            RoundNumber = roundNumber;
            HandNumber = handNumber;
            StreamStateBefore = streamStateBefore;
            StreamStateAfter = streamStateAfter;
            Roll = roll;
            Threshold = threshold;
            Succeeded = succeeded;
            Choice = choice;
            CostPaid = costPaid;
            ResourcesBefore = resourcesBefore;
            ResourcesAfter = resourcesAfter;
            AffectedCardIds = SnapshotCopy.List(affectedCardIds);
            AffectedRealValues = SnapshotCopy.List(affectedRealValues);
            EmittedAnswer = emittedAnswer;
            InformationWasTruthful = informationWasTruthful;
            SuspicionSignal = suspicionSignal;
            EffectExpiration = effectExpiration ?? string.Empty;
        }

        public long Sequence { get; }
        public InternalHistoryKind Kind { get; }
        public string TechnicalCode { get; }
        public string Checkpoint { get; }
        public int CycleNumber { get; }
        public int RoundNumber { get; }
        public int HandNumber { get; }
        public RandomStreamState? StreamStateBefore { get; }
        public RandomStreamState? StreamStateAfter { get; }
        public int? Roll { get; }
        public int? Threshold { get; }
        public bool? Succeeded { get; }
        public ContentDecisionOption? Choice { get; }
        public Money CostPaid { get; }
        public InternalResourceState ResourcesBefore { get; }
        public InternalResourceState ResourcesAfter { get; }
        public IReadOnlyList<CardId> AffectedCardIds { get; }
        public IReadOnlyList<int> AffectedRealValues { get; }
        public bool? EmittedAnswer { get; }
        public bool? InformationWasTruthful { get; }
        public bool? SuspicionSignal { get; }
        public string EffectExpiration { get; }
    }
}
