using System;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Domain.Rules
{
    public sealed class PhaseThreeRules
    {
        public PhaseThreeRules(
            bool isEnabled,
            ISpecialCardCatalog specialCards,
            IGameEventCatalog events,
            IItemCatalog items,
            int voluntaryHitPressure,
            int voluntaryHitAfterLossPressure,
            int allInPressure,
            int allInLossPressure,
            BasisPoints luckyCoinProbability,
            int dealRejectionPressureWhenUnfunded = 16,
            int cigarettePressureRelief = 15,
            int cigaretteLucidityLoss = 30,
            Money? protectedFundsCapacity = null,
            int secondChanceLucidity = 40,
            int surrenderedEyeLucidityMaximum = 70)
        {
            SpecialCards = specialCards ?? throw new ArgumentNullException(nameof(specialCards));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Items = items ?? throw new ArgumentNullException(nameof(items));
            RequireNonNegative(voluntaryHitPressure, nameof(voluntaryHitPressure));
            RequireNonNegative(voluntaryHitAfterLossPressure, nameof(voluntaryHitAfterLossPressure));
            RequireNonNegative(allInPressure, nameof(allInPressure));
            RequireNonNegative(allInLossPressure, nameof(allInLossPressure));
            RequireNonNegative(
                dealRejectionPressureWhenUnfunded,
                nameof(dealRejectionPressureWhenUnfunded));
            RequireNonNegative(cigarettePressureRelief, nameof(cigarettePressureRelief));
            RequireNonNegative(cigaretteLucidityLoss, nameof(cigaretteLucidityLoss));

            Money resolvedCapacity = protectedFundsCapacity ?? Money.FromCoins(20);
            if (resolvedCapacity == Money.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(protectedFundsCapacity));
            }

            if (secondChanceLucidity < 0 || secondChanceLucidity > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(secondChanceLucidity));
            }

            if (surrenderedEyeLucidityMaximum < 0 || surrenderedEyeLucidityMaximum > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(surrenderedEyeLucidityMaximum));
            }

            if (luckyCoinProbability.Value > BasisPoints.Scale)
            {
                throw new ArgumentOutOfRangeException(nameof(luckyCoinProbability));
            }

            IsEnabled = isEnabled;
            VoluntaryHitPressure = voluntaryHitPressure;
            VoluntaryHitAfterLossPressure = voluntaryHitAfterLossPressure;
            AllInPressure = allInPressure;
            AllInLossPressure = allInLossPressure;
            LuckyCoinProbability = luckyCoinProbability;
            DealRejectionPressureWhenUnfunded = dealRejectionPressureWhenUnfunded;
            CigarettePressureRelief = cigarettePressureRelief;
            CigaretteLucidityLoss = cigaretteLucidityLoss;
            ProtectedFundsCapacity = resolvedCapacity;
            SecondChanceLucidity = secondChanceLucidity;
            SurrenderedEyeLucidityMaximum = surrenderedEyeLucidityMaximum;
        }

        public bool IsEnabled { get; }

        public ISpecialCardCatalog SpecialCards { get; }

        public IGameEventCatalog Events { get; }

        public IItemCatalog Items { get; }

        public int VoluntaryHitPressure { get; }

        public int VoluntaryHitAfterLossPressure { get; }

        public int AllInPressure { get; }

        public int AllInLossPressure { get; }

        public BasisPoints LuckyCoinProbability { get; }

        public int DealRejectionPressureWhenUnfunded { get; }

        public int CigarettePressureRelief { get; }

        public int CigaretteLucidityLoss { get; }

        public Money ProtectedFundsCapacity { get; }

        public int SecondChanceLucidity { get; }

        public int SurrenderedEyeLucidityMaximum { get; }

        public static PhaseThreeRules CreateDefault()
        {
            return new PhaseThreeRules(
                true,
                DemoSpecialCardCatalog.CreateDefault(),
                DemoGameEventCatalog.CreateDefault(),
                new DemoItemCatalogFactory().Create(),
                10,
                15,
                15,
                10,
                new BasisPoints(1000));
        }

        public static PhaseThreeRules CreateDisabled()
        {
            return new PhaseThreeRules(
                false,
                DemoSpecialCardCatalog.CreateDefault(),
                DemoGameEventCatalog.CreateDefault(),
                new DemoItemCatalogFactory().Create(),
                10,
                15,
                15,
                10,
                new BasisPoints(1000));
        }

        private static void RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
