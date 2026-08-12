using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Tests.EditMode.Economy
{
    public sealed class BettingTests
    {
        private static BetRules StandardBetRules => new BetRules(
            Money.FromCoins(5),
            Money.FromCoins(20));

        private static PayoutRules StandardPayoutRules => new PayoutRules(
            new BasisPoints(9200),
            new BasisPoints(11000),
            new BasisPoints(12000),
            new BasisPoints(15000));

        [Test]
        public void BetRulesRejectInvalidLimits()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BetRules(Money.Zero, Money.FromCoins(20)));
            Assert.Throws<ArgumentException>(
                () => new BetRules(Money.FromCoins(20), Money.FromCoins(5)));
        }

        [Test]
        public void DefaultBetRulesCannotPlaceAnyBet()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            BetPlacementService service = new BetPlacementService();

            Assert.That(
                service.TryPlace(wallet, Money.Zero, default, out LockedBet standardBet),
                Is.False);
            Assert.That(service.TryPlaceAllIn(wallet, default, out LockedBet allInBet), Is.False);
            Assert.That(standardBet, Is.Null);
            Assert.That(allInBet, Is.Null);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
        }

        [Test]
        public void StandardBetLocksFundsAndIdentifiesTheBet()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            BetPlacementService service = new BetPlacementService();

            bool placed = service.TryPlace(
                wallet,
                Money.FromCoins(10),
                StandardBetRules,
                out LockedBet bet);

            Assert.That(placed, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));
            Assert.That(bet.Amount, Is.EqualTo(Money.FromCoins(10)));
            Assert.That(bet.IsAllIn, Is.False);
            Assert.That(bet.IsSettled, Is.False);
        }

        [TestCase(4)]
        [TestCase(21)]
        [TestCase(51)]
        public void InvalidStandardBetDoesNotMutateWallet(long coins)
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            BetPlacementService service = new BetPlacementService();

            bool placed = service.TryPlace(
                wallet,
                Money.FromCoins(coins),
                StandardBetRules,
                out LockedBet bet);

            Assert.That(placed, Is.False);
            Assert.That(bet, Is.Null);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
        }

        [Test]
        public void AllInLocksTheExactAvailableBalanceAndBypassesOnlyMaximum()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            BetPlacementService service = new BetPlacementService();

            bool placed = service.TryPlaceAllIn(wallet, StandardBetRules, out LockedBet bet);

            Assert.That(placed, Is.True);
            Assert.That(bet.Amount, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(bet.IsAllIn, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
        }

        [Test]
        public void AllInBelowMinimumIsRejectedWithoutMutation()
        {
            Wallet wallet = new Wallet(Money.FromCoins(4));
            BetPlacementService service = new BetPlacementService();

            bool placed = service.TryPlaceAllIn(wallet, StandardBetRules, out LockedBet bet);

            Assert.That(placed, Is.False);
            Assert.That(bet, Is.Null);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(4)));
        }

        [TestCase(BetOutcome.Loss, 4000)]
        [TestCase(BetOutcome.Draw, 5000)]
        [TestCase(BetOutcome.NormalWin, 5920)]
        [TestCase(BetOutcome.InitialTwentyThree, 6200)]
        public void StandardBetSettlementMatchesNormativeBalances(
            BetOutcome outcome,
            long expectedMinorUnits)
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            LockedBet bet = PlaceStandardBet(wallet, Money.FromCoins(10));
            PayoutCalculator calculator = new PayoutCalculator();

            bool settled = calculator.TrySettle(
                bet,
                outcome,
                StandardPayoutRules,
                wallet,
                out Money payout);

            Assert.That(settled, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(expectedMinorUnits)));
            Assert.That(bet.IsSettled, Is.True);
            Assert.That(
                payout,
                Is.EqualTo(Money.FromMinorUnits(expectedMinorUnits - 4000)));
        }

        [TestCase(BetOutcome.NormalWin, 10500)]
        [TestCase(BetOutcome.InitialTwentyThree, 12500)]
        public void AllInSettlementUsesAllInPayouts(
            BetOutcome outcome,
            long expectedMinorUnits)
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            BetPlacementService placement = new BetPlacementService();
            Assert.That(placement.TryPlaceAllIn(wallet, StandardBetRules, out LockedBet bet), Is.True);
            PayoutCalculator calculator = new PayoutCalculator();

            Assert.That(
                calculator.TrySettle(
                    bet,
                    outcome,
                    StandardPayoutRules,
                    wallet,
                    out Money payout),
                Is.True);
            Assert.That(payout, Is.EqualTo(Money.FromMinorUnits(expectedMinorUnits)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(expectedMinorUnits)));
        }

        [Test]
        public void SettlementCanOnlyOccurOnce()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            LockedBet bet = PlaceStandardBet(wallet, Money.FromCoins(10));
            PayoutCalculator calculator = new PayoutCalculator();

            Assert.That(
                calculator.TrySettle(
                    bet,
                    BetOutcome.NormalWin,
                    StandardPayoutRules,
                    wallet,
                    out Money firstPayout),
                Is.True);
            Assert.That(firstPayout, Is.EqualTo(Money.FromMinorUnits(1920)));

            Assert.That(
                calculator.TrySettle(
                    bet,
                    BetOutcome.NormalWin,
                    StandardPayoutRules,
                    wallet,
                    out Money secondPayout),
                Is.False);
            Assert.That(secondPayout, Is.EqualTo(Money.Zero));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(5920)));
        }

        [Test]
        public void UnknownOutcomeIsRejectedWithoutSettlingOrCrediting()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            LockedBet bet = PlaceStandardBet(wallet, Money.FromCoins(10));
            PayoutCalculator calculator = new PayoutCalculator();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => calculator.TrySettle(
                    bet,
                    (BetOutcome)999,
                    StandardPayoutRules,
                    wallet,
                    out Money ignored));
            Assert.That(bet.IsSettled, Is.False);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));
        }

        [Test]
        public void PayoutUsesMoneyHalfUpRounding()
        {
            BetRules rules = new BetRules(Money.FromMinorUnits(1), Money.FromMinorUnits(1));
            Wallet wallet = new Wallet(Money.FromMinorUnits(1));
            BetPlacementService placement = new BetPlacementService();
            Assert.That(
                placement.TryPlace(wallet, Money.FromMinorUnits(1), rules, out LockedBet bet),
                Is.True);
            PayoutRules payouts = new PayoutRules(
                new BasisPoints(5000),
                BasisPoints.Zero,
                BasisPoints.Zero,
                BasisPoints.Zero);
            PayoutCalculator calculator = new PayoutCalculator();

            Assert.That(
                calculator.TrySettle(
                    bet,
                    BetOutcome.NormalWin,
                    payouts,
                    wallet,
                    out Money payout),
                Is.True);
            Assert.That(payout, Is.EqualTo(Money.FromMinorUnits(2)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(2)));
        }

        private static LockedBet PlaceStandardBet(Wallet wallet, Money amount)
        {
            BetPlacementService service = new BetPlacementService();
            Assert.That(service.TryPlace(wallet, amount, StandardBetRules, out LockedBet bet), Is.True);
            return bet;
        }
    }
}
