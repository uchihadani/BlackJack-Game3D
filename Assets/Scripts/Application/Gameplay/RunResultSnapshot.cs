using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Progression;
using TwentyThree.Application.Gameplay.Snapshots;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Application.Gameplay
{
    public sealed class PhaseThreeRunResultSnapshot
    {
        public PhaseThreeRunResultSnapshot(
            PsychologySnapshot psychology,
            IReadOnlyList<ItemStateSnapshot> items,
            bool eyeSurrendered,
            bool lastBreathPriceDoubled,
            bool secondChanceUsed,
            int ruleRoundIndex,
            int roundOrdinal,
            long roundInstanceId,
            bool isExtraordinaryRound)
        {
            Psychology = psychology;
            Items = Array.AsReadOnly(Copy(items));
            EyeSurrendered = eyeSurrendered;
            LastBreathPriceDoubled = lastBreathPriceDoubled;
            SecondChanceUsed = secondChanceUsed;
            RuleRoundIndex = ruleRoundIndex;
            RoundOrdinal = roundOrdinal;
            RoundInstanceId = roundInstanceId;
            IsExtraordinaryRound = isExtraordinaryRound;
        }

        public PsychologySnapshot Psychology { get; }
        public IReadOnlyList<ItemStateSnapshot> Items { get; }
        public bool EyeSurrendered { get; }
        public bool LastBreathPriceDoubled { get; }
        public bool SecondChanceUsed { get; }
        public int RuleRoundIndex { get; }
        public int RoundOrdinal { get; }
        public long RoundInstanceId { get; }
        public bool IsExtraordinaryRound { get; }

        private static ItemStateSnapshot[] Copy(IReadOnlyList<ItemStateSnapshot> source)
        {
            ItemStateSnapshot[] result = new ItemStateSnapshot[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }
}

namespace TwentyThree.Application.Gameplay
{
    public sealed class RunResultSnapshot
    {
        private readonly ReadOnlyCollection<int> _roundSeeds;
        private readonly ReadOnlyCollection<RoundHistoryEntry> _roundHistory;

        public RunResultSnapshot(
            int runSeed,
            RunStatus status,
            Money availableMoney,
            Money protectedMoney,
            Money remainingDebt,
            int cycleNumber,
            int roundNumber,
            int handNumber,
            IReadOnlyList<int> roundSeeds,
            IReadOnlyList<RoundHistoryEntry> roundHistory,
            PhaseThreeRunResultSnapshot phaseThree = null)
        {
            if (status != RunStatus.DemoCompleted &&
                status != RunStatus.DebtDeadlineMissed)
            {
                throw new ArgumentOutOfRangeException(nameof(status));
            }

            if (cycleNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cycleNumber));
            }

            if (roundNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (handNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(handNumber));
            }

            RunSeed = runSeed;
            Status = status;
            AvailableMoney = availableMoney;
            ProtectedMoney = protectedMoney;
            RemainingDebt = remainingDebt;
            CycleNumber = cycleNumber;
            RoundNumber = roundNumber;
            HandNumber = handNumber;
            _roundSeeds = Array.AsReadOnly(Copy(roundSeeds, nameof(roundSeeds)));
            _roundHistory = Array.AsReadOnly(Copy(roundHistory, nameof(roundHistory)));
            PhaseThree = phaseThree;
        }

        public int RunSeed { get; }

        public RunStatus Status { get; }

        public Money AvailableMoney { get; }

        public Money ProtectedMoney { get; }

        public Money RemainingDebt { get; }

        public int CycleNumber { get; }

        public int RoundNumber { get; }

        public int HandNumber { get; }

        public IReadOnlyList<int> RoundSeeds => _roundSeeds;

        public IReadOnlyList<RoundHistoryEntry> RoundHistory => _roundHistory;

        public PhaseThreeRunResultSnapshot PhaseThree { get; }

        private static T[] Copy<T>(IReadOnlyList<T> source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            T[] result = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }
}
