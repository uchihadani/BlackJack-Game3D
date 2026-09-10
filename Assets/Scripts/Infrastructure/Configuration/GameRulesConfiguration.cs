using System;
using System.Collections.Generic;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Rules;
using TwentyThree.Domain.Events;
using TwentyThree.Domain.Items;
using TwentyThree.Domain.SpecialCards;
using UnityEngine;

namespace TwentyThree.Infrastructure.Configuration
{
    [CreateAssetMenu(
        fileName = "GameRulesConfiguration",
        menuName = "23 al Filo/Configuration/Game Rules")]
    public sealed class GameRulesConfiguration : ScriptableObject
    {
        [SerializeField] private long initialMoneyMinorUnits = 5000;
        [SerializeField] private long initialDebtMinorUnits = 20000;
        [SerializeField] private long[] laterCycleDebtMinorUnits = { 40000, 80000, 160000 };
        [SerializeField] private long minimumBetMinorUnits = 500;
        [SerializeField] private long[] maximumBetByRoundMinorUnits = { 2000, 2500, 3000, 3500 };
        [SerializeField] private int interestPerRoundBasisPoints = 1500;
        [SerializeField] private int normalWinBasisPoints = 9200;
        [SerializeField] private int allInWinBasisPoints = 11000;
        [SerializeField] private int initialTwentyThreeBasisPoints = 12000;
        [SerializeField] private int allInInitialTwentyThreeBasisPoints = 15000;
        [SerializeField] private int handsPerRound = 5;
        [SerializeField] private int roundsPerCycle = 4;
        [SerializeField] private int dealerStandThreshold = 17;
        [SerializeField] private int minimumCardsToStartHand = 6;
        [SerializeField] private long[] specialRejectionCosts = { 1000, 2500, 3000, 2500, 1600 };
        [SerializeField] private int[] specialPressureDeltas = { 0, 20, 25, 40, 50 };
        [SerializeField] private int voluntaryHitPressure = 10;
        [SerializeField] private int voluntaryHitAfterLossPressure = 15;
        [SerializeField] private int allInPressure = 15;
        [SerializeField] private int allInLossPressure = 10;
        [SerializeField] private int luckyCoinProbabilityBasisPoints = 1000;
        [SerializeField] private int dealRejectionPressureWhenUnfunded = 16;
        [SerializeField] private int cigarettePressureRelief = 15;
        [SerializeField] private int cigaretteLucidityLoss = 30;
        [SerializeField] private long protectedFundsCapacityMinorUnits = 2000;
        [SerializeField] private int secondChanceLucidity = 40;
        [SerializeField] private int surrenderedEyeLucidityMaximum = 70;
        [SerializeField] private long[] itemPrices = { 4000, 2000, 4000, 5000, 2000, 3500 };
        [SerializeField] private int voicesProbabilityBasisPoints = 1500;
        [SerializeField] private long voicesRejectionCostMinorUnits = 2000;
        [SerializeField] private int voicesTruthProbabilityBasisPoints = 7000;
        [SerializeField] private int falseMessageSignalProbabilityBasisPoints = 8000;
        [SerializeField] private int trueMessageSignalProbabilityBasisPoints = 2000;
        [SerializeField] private int voicesPressureRelief = 15;
        [SerializeField] private int meowProbabilityBasisPoints = 1200;
        [SerializeField] private int meowLucidityLoss = 25;
        [SerializeField] private int distractedProbabilityBasisPoints = 800;
        [SerializeField] private long distractedRejectionCostMinorUnits = 3000;
        [SerializeField] private int distractedLucidityLoss = 30;
        [SerializeField] private int distractedNetGainBonusBasisPoints = 2000;

        public long InitialMoneyMinorUnits => initialMoneyMinorUnits;

        public long InitialDebtMinorUnits => initialDebtMinorUnits;

        public IReadOnlyList<long> LaterCycleDebtMinorUnits => laterCycleDebtMinorUnits;

        public long MinimumBetMinorUnits => minimumBetMinorUnits;

        public IReadOnlyList<long> MaximumBetByRoundMinorUnits => maximumBetByRoundMinorUnits;

        public int InterestPerRoundBasisPoints => interestPerRoundBasisPoints;

        public int NormalWinBasisPoints => normalWinBasisPoints;

        public int AllInWinBasisPoints => allInWinBasisPoints;

        public int InitialTwentyThreeBasisPoints => initialTwentyThreeBasisPoints;

        public int AllInInitialTwentyThreeBasisPoints => allInInitialTwentyThreeBasisPoints;

        public int HandsPerRound => handsPerRound;

        public int RoundsPerCycle => roundsPerCycle;

        public int DealerStandThreshold => dealerStandThreshold;

        public int MinimumCardsToStartHand => minimumCardsToStartHand;

        public IReadOnlyList<long> SpecialRejectionCosts => specialRejectionCosts;

        public IReadOnlyList<int> SpecialPressureDeltas => specialPressureDeltas;

        public IReadOnlyList<long> ItemPrices => itemPrices;

        public GameRules CreateRules()
        {
            if (laterCycleDebtMinorUnits == null)
            {
                throw new InvalidOperationException("Later cycle debts are required.");
            }

            if (maximumBetByRoundMinorUnits == null)
            {
                throw new InvalidOperationException("Maximum bets by round are required.");
            }

            Money[] debts = new Money[checked(laterCycleDebtMinorUnits.Length + 1)];
            debts[0] = Money.FromMinorUnits(initialDebtMinorUnits);
            for (int index = 0; index < laterCycleDebtMinorUnits.Length; index++)
            {
                debts[index + 1] = Money.FromMinorUnits(laterCycleDebtMinorUnits[index]);
            }

            Money[] maximumBets = new Money[maximumBetByRoundMinorUnits.Length];
            for (int index = 0; index < maximumBetByRoundMinorUnits.Length; index++)
            {
                maximumBets[index] = Money.FromMinorUnits(maximumBetByRoundMinorUnits[index]);
            }

            PayoutRules payouts = new PayoutRules(
                new BasisPoints(normalWinBasisPoints),
                new BasisPoints(allInWinBasisPoints),
                new BasisPoints(initialTwentyThreeBasisPoints),
                new BasisPoints(allInInitialTwentyThreeBasisPoints));

            ValidatePhaseThreeArrays();
            DemoSpecialCardCatalog specialCards = new DemoSpecialCardCatalog(
                Money.FromMinorUnits(specialRejectionCosts[0]),
                Money.FromMinorUnits(specialRejectionCosts[1]),
                Money.FromMinorUnits(specialRejectionCosts[2]),
                Money.FromMinorUnits(specialRejectionCosts[3]),
                Money.FromMinorUnits(specialRejectionCosts[4]),
                specialPressureDeltas[1],
                specialPressureDeltas[2],
                specialPressureDeltas[3],
                specialPressureDeltas[4]);
            DemoGameEventCatalog events = new DemoGameEventCatalog(
                new BasisPoints(voicesProbabilityBasisPoints),
                Money.FromMinorUnits(voicesRejectionCostMinorUnits),
                new BasisPoints(voicesTruthProbabilityBasisPoints),
                new BasisPoints(falseMessageSignalProbabilityBasisPoints),
                new BasisPoints(trueMessageSignalProbabilityBasisPoints),
                voicesPressureRelief,
                new BasisPoints(meowProbabilityBasisPoints),
                meowLucidityLoss,
                new BasisPoints(distractedProbabilityBasisPoints),
                Money.FromMinorUnits(distractedRejectionCostMinorUnits),
                distractedLucidityLoss,
                new BasisPoints(distractedNetGainBonusBasisPoints));
            IItemCatalog items = new DemoItemCatalogFactory(
                Money.FromMinorUnits(itemPrices[0]),
                Money.FromMinorUnits(itemPrices[1]),
                Money.FromMinorUnits(itemPrices[2]),
                Money.FromMinorUnits(itemPrices[3]),
                Money.FromMinorUnits(itemPrices[4]),
                Money.FromMinorUnits(itemPrices[5])).Create();
            PhaseThreeRules phaseThree = new PhaseThreeRules(
                true,
                specialCards,
                events,
                items,
                voluntaryHitPressure,
                voluntaryHitAfterLossPressure,
                allInPressure,
                allInLossPressure,
                new BasisPoints(luckyCoinProbabilityBasisPoints),
                dealRejectionPressureWhenUnfunded,
                cigarettePressureRelief,
                cigaretteLucidityLoss,
                Money.FromMinorUnits(protectedFundsCapacityMinorUnits),
                secondChanceLucidity,
                surrenderedEyeLucidityMaximum);

            return new GameRules(
                Money.FromMinorUnits(initialMoneyMinorUnits),
                debts,
                Money.FromMinorUnits(minimumBetMinorUnits),
                maximumBets,
                new BasisPoints(interestPerRoundBasisPoints),
                payouts,
                handsPerRound,
                roundsPerCycle,
                dealerStandThreshold,
                minimumCardsToStartHand,
                phaseThree);
        }

        private void ValidatePhaseThreeArrays()
        {
            if (specialRejectionCosts == null || specialRejectionCosts.Length != 5)
            {
                throw new InvalidOperationException(
                    "Exactly five special rejection costs are required.");
            }

            if (specialPressureDeltas == null || specialPressureDeltas.Length != 5)
            {
                throw new InvalidOperationException(
                    "Exactly five special pressure deltas are required.");
            }

            if (itemPrices == null || itemPrices.Length != 6)
            {
                throw new InvalidOperationException(
                    "Six item price values are required, including both IT-02 prices.");
            }
        }
    }
}
