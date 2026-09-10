using System;
using System.Collections.Generic;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Progression;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Tests.EditMode.Progression
{
    public sealed class ExtraordinaryRoundProgressionTests
    {
        [Test]
        public void OrdinaryRoundsExposeStableRuleOrdinalAndUniqueInstanceIdentity()
        {
            RunProgression progression = CreateProgression();
            HashSet<long> instanceIds = new HashSet<long> { progression.RoundInstanceId };

            Assert.That(progression.RuleRoundIndex, Is.EqualTo(1));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(1));
            Assert.That(progression.IsExtraordinary, Is.False);
            Assert.That(progression.SecondChanceUsed, Is.False);

            for (int expectedRound = 2; expectedRound <= 4; expectedRound++)
            {
                CompleteCurrentRound(progression);
                progression.CloseRound();

                Assert.That(progression.RuleRoundIndex, Is.EqualTo(expectedRound));
                Assert.That(progression.RoundOrdinal, Is.EqualTo(expectedRound));
                Assert.That(progression.IsExtraordinary, Is.False);
                Assert.That(instanceIds.Add(progression.RoundInstanceId), Is.True);
            }
        }

        [Test]
        public void DeferredFourthRoundClosureAppliesInterestAndOpensFailureWindow()
        {
            RunProgression progression = ReachFourthRound();
            CompleteCurrentRound(progression);

            Money interest = progression.CloseRoundDeferred();

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(4563)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromMinorUnits(34981)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlinePending));
            Assert.That(progression.RuleRoundIndex, Is.EqualTo(4));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(4));
            Assert.That(progression.IsExtraordinary, Is.False);
            Assert.That(progression.CanCommitDebtDeadline, Is.True);
            Assert.That(progression.CanStartExtraordinaryRound, Is.True);
            Assert.That(progression.CanStartHand, Is.False);
            Assert.That(progression.CanPayDebt, Is.False);
            Assert.That(progression.CanCloseRound, Is.False);
            Assert.That(progression.CanAbandonRound, Is.False);
            Assert.That(progression.RemainingRoundsInCycle, Is.Zero);
        }

        [Test]
        public void PendingDebtDeadlineCanBeCommittedExactlyOnce()
        {
            RunProgression progression = ReachPendingDeadline();

            progression.CommitDebtDeadlineMissed();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.CanCommitDebtDeadline, Is.False);
            Assert.That(progression.CanStartExtraordinaryRound, Is.False);
            Assert.That(
                progression.CommitDebtDeadlineMissed,
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ExtraordinaryRoundPreservesDebtAndUsesFourthRoundRulesWithFifthOrdinal()
        {
            RunProgression progression = ReachPendingDeadline();
            Money debtBeforeActivation = progression.RemainingDebt;
            long fourthRoundInstanceId = progression.RoundInstanceId;

            progression.StartExtraordinaryRound();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.RuleRoundIndex, Is.EqualTo(4));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(4));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(5));
            Assert.That(progression.RoundInstanceId, Is.Not.EqualTo(fourthRoundInstanceId));
            Assert.That(progression.IsExtraordinary, Is.True);
            Assert.That(progression.SecondChanceUsed, Is.True);
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.CurrentHandNumber, Is.EqualTo(1));
            Assert.That(progression.RemainingHandsInRound, Is.EqualTo(5));
            Assert.That(progression.RemainingRoundsInCycle, Is.EqualTo(1));
            Assert.That(progression.CurrentMaximumBet, Is.EqualTo(Money.FromCoins(35)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(debtBeforeActivation));
        }

        [Test]
        public void CompletedExtraordinaryRoundAppliesOneMoreInterestAndCommitsFailure()
        {
            RunProgression progression = ReachPendingDeadline();
            progression.StartExtraordinaryRound();
            CompleteCurrentRound(progression);

            Money interest = progression.CloseRoundDeferred();

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(5247)));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromMinorUnits(40228)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.IsExtraordinary, Is.True);
            Assert.That(progression.RoundOrdinal, Is.EqualTo(5));
            Assert.That(progression.CanCommitDebtDeadline, Is.False);
            Assert.That(progression.CanStartExtraordinaryRound, Is.False);
        }

        [Test]
        public void AbandonedExtraordinaryRoundCannotOpenASecondFailureWindow()
        {
            RunProgression progression = ReachPendingDeadline();
            progression.StartExtraordinaryRound();
            progression.RecordCompletedHand();

            Money interest = progression.AbandonRoundDeferred();

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(5247)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.SecondChanceUsed, Is.True);
            Assert.That(progression.CanStartExtraordinaryRound, Is.False);
        }

        [Test]
        public void FundingRequiredHasTheOnlyExplicitZeroHandAbandonmentPath()
        {
            RunProgression progression = CreateProgression();

            Assert.That(progression.CanAbandonRound, Is.False);
            Assert.That(progression.CanAbandonRoundFromFundingRequired, Is.True);
            Assert.That(
                () => progression.AbandonRound(),
                Throws.TypeOf<InvalidOperationException>());

            Money interest = progression.AbandonRoundFromFundingRequired();

            Assert.That(interest, Is.EqualTo(Money.FromCoins(30)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(2));
            Assert.That(progression.RoundInstanceId, Is.EqualTo(2));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
        }

        [Test]
        public void ZeroHandAbandonmentOfFourthRoundOpensTheFailureWindow()
        {
            RunProgression progression = CreateProgression();

            for (int round = 1; round <= 3; round++)
            {
                progression.AbandonRoundFromFundingRequired();
            }

            Money interest = progression.AbandonRoundFromFundingRequired();

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(4563)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlinePending));
            Assert.That(progression.CompletedHandsInRound, Is.Zero);
            Assert.That(progression.CanStartExtraordinaryRound, Is.True);
        }

        [Test]
        public void ZeroHandAbandonmentOfExtraordinaryRoundCommitsFailureDirectly()
        {
            RunProgression progression = ReachPendingDeadline();
            progression.StartExtraordinaryRound();

            Money interest = progression.AbandonRoundFromFundingRequired();

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(5247)));
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlineMissed));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(5));
            Assert.That(progression.IsExtraordinary, Is.True);
        }

        [Test]
        public void SecondChanceRemainsConsumedAfterPayingDuringExtraordinaryRound()
        {
            RunProgression progression = ReachPendingDeadline();
            progression.StartExtraordinaryRound();
            progression.RecordCompletedHand();
            Wallet wallet = new Wallet(progression.RemainingDebt);

            Assert.That(progression.PayDebt(wallet, progression.RemainingDebt), Is.True);

            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(2));
            Assert.That(progression.RuleRoundIndex, Is.EqualTo(1));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(1));
            Assert.That(progression.IsExtraordinary, Is.False);
            Assert.That(progression.SecondChanceUsed, Is.True);

            ReachPendingDeadlineInCurrentCycle(progression);

            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlinePending));
            Assert.That(progression.CanStartExtraordinaryRound, Is.False);
            Assert.That(
                progression.StartExtraordinaryRound,
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void CombinedDebtPaymentAdvancesCycleAsOneTransaction()
        {
            RunProgression progression = CreateProgression();
            Wallet wallet = new Wallet(Money.FromCoins(150), Money.FromCoins(50));
            progression.RecordCompletedHand();

            DebtPaymentResult result = progression.PayDebt(
                wallet,
                new DebtPaymentAllocation(Money.FromCoins(150), Money.FromCoins(50)));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Paid, Is.EqualTo(Money.FromCoins(200)));
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
            Assert.That(wallet.Protected, Is.EqualTo(Money.Zero));
            Assert.That(progression.CurrentCycleNumber, Is.EqualTo(2));
            Assert.That(progression.RemainingDebt, Is.EqualTo(Money.FromCoins(400)));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(1));
            Assert.That(progression.RoundInstanceId, Is.EqualTo(2));
        }

        [Test]
        public void FailureWindowRejectsPaymentsAndUnrelatedRoundMutations()
        {
            RunProgression progression = ReachPendingDeadline();
            Wallet wallet = new Wallet(Money.FromCoins(500));

            DebtPaymentResult payment = progression.PayDebt(
                wallet,
                new DebtPaymentAllocation(Money.FromCoins(1), Money.Zero));

            Assert.That(payment.Failure, Is.EqualTo(DebtPaymentFailure.PaymentWindowClosed));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(500)));
            Assert.That(
                progression.RecordCompletedHand,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                progression.RequestRoundClosure,
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.CloseRoundDeferred(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.AbandonRoundDeferred(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(
                () => progression.AbandonRoundFromFundingRequired(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(progression.Status, Is.EqualTo(RunStatus.DebtDeadlinePending));
        }

        [Test]
        public void DeferredClosureOfAnOrdinaryRoundStillAdvancesImmediately()
        {
            RunProgression progression = CreateProgression();
            CompleteCurrentRound(progression);

            progression.CloseRoundDeferred();

            Assert.That(progression.Status, Is.EqualTo(RunStatus.Active));
            Assert.That(progression.CurrentRoundNumber, Is.EqualTo(2));
            Assert.That(progression.RoundOrdinal, Is.EqualTo(2));
            Assert.That(progression.RoundInstanceId, Is.EqualTo(2));
            Assert.That(progression.CanCommitDebtDeadline, Is.False);
        }

        private static RunProgression ReachPendingDeadline()
        {
            RunProgression progression = ReachFourthRound();
            CompleteCurrentRound(progression);
            progression.CloseRoundDeferred();
            return progression;
        }

        private static RunProgression ReachFourthRound()
        {
            RunProgression progression = CreateProgression();

            for (int round = 1; round <= 3; round++)
            {
                CompleteCurrentRound(progression);
                progression.CloseRound();
            }

            return progression;
        }

        private static void ReachPendingDeadlineInCurrentCycle(RunProgression progression)
        {
            for (int round = 1; round <= 3; round++)
            {
                CompleteCurrentRound(progression);
                progression.CloseRound();
            }

            CompleteCurrentRound(progression);
            progression.CloseRoundDeferred();
        }

        private static RunProgression CreateProgression()
        {
            return new RunProgression(new GameRules(
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
                6));
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
