using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Progression;

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
            IReadOnlyList<RoundHistoryEntry> roundHistory)
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
