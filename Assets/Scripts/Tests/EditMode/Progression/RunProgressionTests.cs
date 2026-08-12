using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Tests.EditMode.Progression
{
    public sealed class RunProgressionTests
    {
        [Test]
        public void NewRunStartsAtTheFirstHandOfTheFirstDebtCycle()
        {
            RunProgression progression = CreateProgression();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(1));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.RemainingHandsInRound, Is.EqualTo(5));
            Assert.That(progression.RemainingRoundsInCycle, Is.EqualTo(4));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(progression.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(20)));
            Assert.That(progression.CanStartHand, Is.True);
            Assert.That(progression.CanPayDebt, Is.False);
            Assert.That(progression.CanCloseRound, Is.False);
            Assert.That(progression.CanRequestRoundClosure, Is.False);
            Assert.That(progression.CanAbandonRound, Is.False);
        }

        [Test]
        public void CompletedHandOpensDebtPaymentAndAbandonmentWindow()
        {
            RunProgression progression = CreateProgression();

            progression.RecordCompletedHand();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(1));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(2));
            Assert.That(progression.RemainingHandsInRound, Is.EqualTo(4));
            Assert.That(progression.CanStartHand, Is.True);
            Assert.That(progression.CanPayDebt, Is.True);
            Assert.That(progression.CanAbandonRound, Is.True);
            Assert.That(progression.CanCloseRound, Is.False);
            Assert.That(progression.CanRequestRoundClosure, Is.True);
        }

        [Test]
        public void FifthCompletedHandRequiresExplicitRoundClosureAndStillAllowsPayment()
        {
            RunProgression progression = CreateProgression();
            CompleteCurrentRound(progression);

            Assert.That(progression.Status, Is.EqualTo(RunStatus.RoundClosurePending));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(5));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(5));
            Assert.That(progression.RemainingHandsInRound, Is.Zero);
            Assert.That(progression.CanStartHand, Is.False);
            Assert.That(progression.CanPayDebt, Is.True);
            Assert.That(progression.CanCloseRound, Is.True);
            Assert.That(progression.CanRequestRoundClosure, Is.False);
            Assert.That(progression.CanAbandonRound, Is.False);
            Assert.That(
                progression.RecordCompletedHand,
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void PartialPaymentDebitsWalletAndReducesRemainingDebt()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(50));
            progression.RecordCompletedHand();

            bool paid = progression.PayDebt(wallet, Money.FromCoins(50));

            Assert.That(paid, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(150)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(1));
        }

        [Test]
        public void PaymentCanUseProtectedFundsWithoutTouchingAvailableMoney()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(10), Money.FromCoins(30));
            progression.RecordCompletedHand();

            bool paid = progression.PayDebtFromProtected(wallet, Money.FromCoins(30));

            Assert.That(paid, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(10)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.Zero));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(170)));
        }

        [Test]
        public void InvalidPaymentsLeaveWalletDebtAndProgressionUnchanged()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(20));

            Assert.That(progression.PayDebt(wallet, Money.FromCoins(10)), Is.False);
            progression.RecordCompletedHand();
            Assert.That(progression.PayDebt(wallet, Money.Zero), Is.False);
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(30)), Is.False);
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(201)), Is.False);

            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(20)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(1));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(1));
        }

        [Test]
        public void PayingACompletedCycleImmediatelyStartsTheNextConfiguredCycle()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(200));
            progression.RecordCompletedHand();

            bool paid = progression.PayDebt(wallet, Money.FromCoins(200));

            Assert.That(paid, Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(2));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(400)));
            Assert.That(progression.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(20)));
            Assert.That(progression.CanPayDebt, Is.False);
        }

        [Test]
        public void PayingAtRoundClosureSkipsInterestAndStartsNextCycle()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(200));
            CompleteCurrentRound(progression);

            bool paid = progression.PayDebt(wallet, Money.FromCoins(200));

            Assert.That(paid, Is.True);
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(2));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(400)));
        }

        [Test]
        public void ClosingRoundAppliesInterestToRemainingDebtAndAdvancesRound()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(80));
            progression.RecordCompletedHand();
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(80)), Is.True);
            for (int hand = 1; hand < 5; hand++)
            {
                progression.RecordCompletedHand();
            }

            Money interest = progression.CloseRound();

            Assert.That(interest, Is.EqualTo(Money.FromCoins(18)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(138)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.RemainingRoundsInCycle, Is.EqualTo(3));
            Assert.That(progression.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(25)));
        }

        [Test]
        public void EarlyRuleClosurePreservesPaymentWindowBeforeApplyingInterest()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(80));
            progression.RecordCompletedHand();
            progression.RecordCompletedHand();

            progression.RequestRoundClosure();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.RoundClosurePending));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(2));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(2));
            Assert.That(progression.RemainingHandsInRound, Is.Zero);
            Assert.That(progression.RemainingRoundsInCycle, Is.EqualTo(3));
            Assert.That(progression.CanStartHand, Is.False);
            Assert.That(progression.CanPayDebt, Is.True);
            Assert.That(progression.CanCloseRound, Is.True);
            Assert.That(progression.CanRequestRoundClosure, Is.False);
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(80)), Is.True);

            Money interest = progression.CloseRound();

            Assert.That(interest, Is.EqualTo(Money.FromCoins(18)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(138)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
        }

        [Test]
        public void EarlyRuleClosureIsRejectedBeforeAnyCompletedHand()
        {
            RunProgression progression = CreateProgression();

            Assert.That(
                progression.RequestRoundClosure,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
        }

        [Test]
        public void InterestUsesConfiguredHalfUpMinorUnitRoundingAcrossRounds()
        {
            RunProgression progression = CreateProgression();

            for (int round = 1; round <= 3; round++)
            {
                CompleteCurrentRound(progression);
                progression.CloseRound();
            }

            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromMinorUnits(30418)));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(4));
        }

        [Test]
        public void FourthUnpaidRoundEntersDebtDeadlineMissed()
        {
            RunProgression progression = CreateProgression();

            for (int round = 1; round <= 4; round++)
            {
                CompleteCurrentRound(progression);
                progression.CloseRound();
            }

            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(1));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(4));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(5));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromMinorUnits(34981)));
            Assert.That(progression.RemainingHandsInRound, Is.Zero);
            Assert.That(progression.RemainingRoundsInCycle, Is.Zero);
            Assert.That(progression.CanStartHand, Is.False);
            Assert.That(progression.CanPayDebt, Is.False);
            Assert.That(progression.CanCloseRound, Is.False);
            Assert.That(progression.CanRequestRoundClosure, Is.False);
            Assert.That(progression.CanAbandonRound, Is.False);
        }

        [Test]
        public void AbandoningAfterCompletedHandConsumesRoundAndAppliesInterest()
        {
            RunProgression progression = CreateProgression();
            progression.RecordCompletedHand();
            progression.RecordCompletedHand();

            Money interest = progression.AbandonRound();

            Assert.That(interest, Is.EqualTo(Money.FromCoins(30)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(230)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(25)));
        }

        [Test]
        public void AbandoningTheFourthUnpaidRoundEntersDebtDeadlineMissed()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(1));

            for (int round = 1; round <= 4; round++)
            {
                progression.RecordCompletedHand();
                progression.AbandonRound();
            }

            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromMinorUnits(34981)));
            Assert.That(
                progression.RecordCompletedHand,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(1)), Is.False);
            Assert.That(
                progression.RequestRoundClosure,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.CloseRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.AbandonRound(),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void AbandonmentIsRejectedBeforeAHandAndAfterNaturalRoundCompletion()
        {
            RunProgression progression = CreateProgression();

            Assert.That(
                () => progression.AbandonRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                progression.RequestRoundClosure,
                Throws.TypeOf<InvalidOperationException>());
            CompleteCurrentRound(progression);
            Assert.That(
                () => progression.AbandonRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.Status, Is.EqualTo(RunStatus.RoundClosurePending));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
        }

        [Test]
        public void RoundClosureIsRejectedUntilEveryHandWasCompleted()
        {
            RunProgression progression = CreateProgression();
            progression.RecordCompletedHand();

            Assert.That(
                () => progression.CloseRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(1));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(1));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
        }

        [Test]
        public void PayingAllFourConfiguredDebtsCompletesDemoWithoutCreatingAnotherDebt()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(3000));
            Money[] debts =
            {
                Money.FromCoins(200),
                Money.FromCoins(400),
                Money.FromCoins(800),
                Money.FromCoins(1600)
            };

            foreach (Money debt in debts)
            {
                progression.RecordCompletedHand();
                Assert.That(progression.PayDebt(wallet, debt), Is.True);
            }

            Assert.That(progression.Status, Is.EqualTo(RunStatus.DemoCompleted));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(4));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.Zero));
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
            Assert.That(progression.RemainingHandsInRound, Is.Zero);
            Assert.That(progression.RemainingRoundsInCycle, Is.Zero);
            Assert.That(progression.CanStartHand, Is.False);
            Assert.That(progression.CanPayDebt, Is.False);
            Assert.That(progression.CanCloseRound, Is.False);
            Assert.That(progression.CanRequestRoundClosure, Is.False);
            Assert.That(progression.CanAbandonRound, Is.False);
        }

        [Test]
        public void TerminalStatesRejectFurtherMutations()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(3000));
            foreach (Money debt in new[]
                     {
                         Money.FromCoins(200),
                         Money.FromCoins(400),
                         Money.FromCoins(800),
                         Money.FromCoins(1600)
                     })
            {
                progression.RecordCompletedHand();
                progression.PayDebt(wallet, debt);
            }

            Assert.That(
                progression.RecordCompletedHand,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.PayDebt(wallet, Money.FromCoins(1)), Is.False);
            Assert.That(
                () => progression.CloseRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.AbandonRound(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                progression.RequestRoundClosure,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DemoCompleted));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.Zero));
        }

        [Test]
        public void NullWalletIsRejectedWithoutMutatingProgression()
        {
            RunProgression progression = CreateProgression();
            progression.RecordCompletedHand();

            Assert.That(
                () => progression.PayDebt(null, Money.FromCoins(1)),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(progression.CompletedHandsInRound, Is.EqualTo(1));
        }

        private static RunProgression CreateProgression()
        {
            return new RunProgression(CreateRules());
        }

        private static GameRules CreateRules()
        {
            return new GameRules(
                Money.FromCoins(50),
                new[]
                {
                    Money.FromCoins(200),
                    Money.FromCoins(400),
                    Money.FromCoins(800),
                    Money.FromCoins(1600)
                },
                Money.FromCoins(5),
                new[]
                {
                    Money.FromCoins(20),
                    Money.FromCoins(25),
                    Money.FromCoins(30),
                    Money.FromCoins(35)
                },
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

        private static void CompleteCurrentRound(RunProgression progression)
        {
            for (int hand = progression.CompletedHandsInRound; hand < 5; hand++)
            {
                progression.RecordCompletedHand();
            }
        }
    }
}
