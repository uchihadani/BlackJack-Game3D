using System;
using System.Collections.Generic;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Tests.EditMode.Progression
{
    public sealed class GameRulesTests
    {
        [Test]
        public void ApprovedConfigurationIsExposedWithoutChangingUnits()
        {
            GameRules rules = CreateRules();

            Assert.That(rules.InitialMoney, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(rules.MinimumBet, Is.EqualTo(Money.FromCoins(5)));
            Assert.That(rules.DebtsByCycle, Is.EqualTo(new[]
            {
                Money.FromCoins(200),
                Money.FromCoins(400),
                Money.FromCoins(800),
                Money.FromCoins(1600)
            }));
            Assert.That(rules.MaximumBetsByRound, Is.EqualTo(new[]
            {
                Money.FromCoins(20),
                Money.FromCoins(25),
                Money.FromCoins(30),
                Money.FromCoins(35)
            }));
            Assert.That(rules.InterestPerRound, Is.EqualTo(new BasisPoints(1500)));
            Assert.That(rules.Payouts.NormalWin, Is.EqualTo(new BasisPoints(9200)));
            Assert.That(rules.Payouts.AllInWin, Is.EqualTo(new BasisPoints(11000)));
            Assert.That(rules.Payouts.InitialTwentyThree, Is.EqualTo(new BasisPoints(12000)));
            Assert.That(rules.Payouts.AllInInitialTwentyThree, Is.EqualTo(new BasisPoints(15000)));
            Assert.That(rules.HandsPerRound, Is.EqualTo(5));
            Assert.That(rules.RoundsPerCycle, Is.EqualTo(4));
            Assert.That(rules.DealerStandThreshold, Is.EqualTo(17));
            Assert.That(rules.MinimumCardsToStartHand, Is.EqualTo(6));
            Assert.That(rules.CycleCount, Is.EqualTo(4));
            Assert.That(rules.GetDebtForCycle(4), Is.EqualTo(Money.FromCoins(1600)));
            Assert.That(rules.GetMaximumBetForRound(4), Is.EqualTo(Money.FromCoins(35)));
        }

        [Test]
        public void ConstructorCopiesSchedules()
        {
            Money[] debts = CreateDebtSchedule();
            Money[] maximumBets = CreateMaximumBetSchedule();
            GameRules rules = CreateRules(debts, maximumBets);

            debts[0] = Money.FromCoins(999);
            maximumBets[0] = Money.FromCoins(999);

            Assert.That(rules.GetDebtForCycle(1), Is.EqualTo(Money.FromCoins(200)));
            Assert.That(rules.GetMaximumBetForRound(1), Is.EqualTo(Money.FromCoins(20)));
            Assert.That(
                () => ((IList<Money>)rules.DebtsByCycle)[0] = Money.FromCoins(999),
                Throws.TypeOf<NotSupportedException>());
            Assert.That(
                () => ((IList<Money>)rules.MaximumBetsByRound)[0] = Money.FromCoins(999),
                Throws.TypeOf<NotSupportedException>());
        }

        [TestCase(0)]
        [TestCase(5)]
        public void DebtLookupRejectsInvalidCycleNumbers(int cycleNumber)
        {
            GameRules rules = CreateRules();

            Assert.That(
                () => rules.GetDebtForCycle(cycleNumber),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(5)]
        public void MaximumBetLookupRejectsInvalidRoundNumbers(int roundNumber)
        {
            GameRules rules = CreateRules();

            Assert.That(
                () => rules.GetMaximumBetForRound(roundNumber),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ConstructorRejectsInvalidMoneyAndSchedules()
        {
            Assert.That(
                () => CreateRules(initialMoney: Money.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CreateRules(minimumBet: Money.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CreateRules(initialMoney: Money.FromCoins(4)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CreateRulesWithSchedules(null, CreateMaximumBetSchedule()),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => CreateRulesWithSchedules(CreateDebtSchedule(), null),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(
                () => CreateRulesWithSchedules(Array.Empty<Money>(), CreateMaximumBetSchedule()),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CreateRulesWithSchedules(CreateDebtSchedule(), Array.Empty<Money>()),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CreateRules(debts: new[] { Money.Zero }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => CreateRules(maximumBets: new[]
                {
                    Money.FromCoins(20),
                    Money.FromCoins(25),
                    Money.FromCoins(30)
                }),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => CreateRules(maximumBets: new[]
                {
                    Money.FromCoins(4),
                    Money.FromCoins(25),
                    Money.FromCoins(30),
                    Money.FromCoins(35)
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(0, 4, 17, 6)]
        [TestCase(5, 0, 17, 6)]
        [TestCase(5, 4, 0, 6)]
        [TestCase(5, 4, 24, 6)]
        [TestCase(5, 4, 17, 0)]
        [TestCase(5, 4, 17, 5)]
        [TestCase(5, 4, 17, 53)]
        public void ConstructorRejectsInvalidNumericRules(
            int handsPerRound,
            int roundsPerCycle,
            int dealerStandThreshold,
            int minimumCardsToStartHand)
        {
            Assert.That(
                () => CreateRules(
                    handsPerRound: handsPerRound,
                    roundsPerCycle: roundsPerCycle,
                    dealerStandThreshold: dealerStandThreshold,
                    minimumCardsToStartHand: minimumCardsToStartHand),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void StandardDeckSizeIsAValidStartThreshold()
        {
            GameRules rules = CreateRules(minimumCardsToStartHand: 52);

            Assert.That(rules.MinimumCardsToStartHand, Is.EqualTo(52));
        }

        private static GameRules CreateRules(
            IReadOnlyList<Money> debts = null,
            IReadOnlyList<Money> maximumBets = null,
            Money? initialMoney = null,
            Money? minimumBet = null,
            int handsPerRound = 5,
            int roundsPerCycle = 4,
            int dealerStandThreshold = 17,
            int minimumCardsToStartHand = 6)
        {
            return new GameRules(
                initialMoney ?? Money.FromCoins(50),
                debts ?? CreateDebtSchedule(),
                minimumBet ?? Money.FromCoins(5),
                maximumBets ?? CreateMaximumBetSchedule(),
                new BasisPoints(1500),
                new PayoutRules(
                    new BasisPoints(9200),
                    new BasisPoints(11000),
                    new BasisPoints(12000),
                    new BasisPoints(15000)),
                handsPerRound,
                roundsPerCycle,
                dealerStandThreshold,
                minimumCardsToStartHand);
        }

        private static Money[] CreateDebtSchedule()
        {
            return new[]
            {
                Money.FromCoins(200),
                Money.FromCoins(400),
                Money.FromCoins(800),
                Money.FromCoins(1600)
            };
        }

        private static Money[] CreateMaximumBetSchedule()
        {
            return new[]
            {
                Money.FromCoins(20),
                Money.FromCoins(25),
                Money.FromCoins(30),
                Money.FromCoins(35)
            };
        }

        private static GameRules CreateRulesWithSchedules(
            IReadOnlyList<Money> debts,
            IReadOnlyList<Money> maximumBets)
        {
            return new GameRules(
                Money.FromCoins(50),
                debts,
                Money.FromCoins(5),
                maximumBets,
                new BasisPoints(1500),
                new PayoutRules(
                    new BasisPoints(9200),
                    new BasisPoints(11000),
                    new BasisPoints(12000),
                    new BasisPoints(15000)),
                5,
                4,
                17,
                6);
        }
    }
}
