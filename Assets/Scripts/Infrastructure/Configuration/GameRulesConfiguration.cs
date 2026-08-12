using System;
using System.Collections.Generic;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Rules;
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
                minimumCardsToStartHand);
        }
    }
}
