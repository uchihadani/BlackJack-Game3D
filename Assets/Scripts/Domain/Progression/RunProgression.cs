using System;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Rules;

namespace TwentyThree.Domain.Progression
{
    public sealed class RunProgression
    {
        private readonly GameRules _rules;
        private DebtAccount _debt;
        private int _completedHandsInRound;

        public RunProgression(GameRules rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            CurrentCycleNumber = 1;
            CurrentRoundNumber = 1;
            RoundOrdinal = 1;
            RoundInstanceId = 1;
            Status = RunStatus.Active;
            _debt = new DebtAccount(_rules.GetDebtForCycle(CurrentCycleNumber));
        }

        public RunStatus Status { get; private set; }

        public int CurrentCycleNumber { get; private set; }

        public int CurrentRoundNumber { get; private set; }

        public int RuleRoundIndex => CurrentRoundNumber;

        public int RoundOrdinal { get; private set; }

        public long RoundInstanceId { get; private set; }

        public bool IsExtraordinary { get; private set; }

        public bool SecondChanceUsed { get; private set; }

        public int CompletedHandsInRound => _completedHandsInRound;

        public int CurrentHandNumber => CanStartHand
            ? _completedHandsInRound + 1
            : _completedHandsInRound;

        public int RemainingHandsInRound => CanStartHand
            ? _rules.HandsPerRound - _completedHandsInRound
            : 0;

        public int RemainingRoundsInCycle => Status switch
        {
            RunStatus.Active => _rules.RoundsPerCycle - CurrentRoundNumber + 1,
            RunStatus.RoundClosurePending => _rules.RoundsPerCycle - CurrentRoundNumber,
            _ => 0
        };

        public Money RemainingDebt => _debt.Remaining;

        public Money CurrentMaximumBet => _rules.GetMaximumBetForRound(CurrentRoundNumber);

        public bool CanStartHand =>
            Status == RunStatus.Active && _completedHandsInRound < _rules.HandsPerRound;

        public bool CanPayDebt =>
            (Status == RunStatus.Active || Status == RunStatus.RoundClosurePending) &&
            _completedHandsInRound > 0;

        public bool CanCloseRound => Status == RunStatus.RoundClosurePending;

        public bool CanRequestRoundClosure =>
            Status == RunStatus.Active && _completedHandsInRound > 0;

        public bool CanAbandonRound =>
            Status == RunStatus.Active &&
            _completedHandsInRound > 0 &&
            _completedHandsInRound < _rules.HandsPerRound;

        public bool CanAbandonRoundFromFundingRequired =>
            Status == RunStatus.Active && _completedHandsInRound == 0;

        public bool CanStartExtraordinaryRound =>
            Status == RunStatus.DebtDeadlinePending && !SecondChanceUsed;

        public bool CanCommitDebtDeadline => Status == RunStatus.DebtDeadlinePending;

        public void RecordCompletedHand()
        {
            if (!CanStartHand)
            {
                throw new InvalidOperationException("A hand cannot be completed in the current run state.");
            }

            _completedHandsInRound++;
            if (_completedHandsInRound == _rules.HandsPerRound)
            {
                Status = RunStatus.RoundClosurePending;
            }
        }

        public void RequestRoundClosure()
        {
            if (!CanRequestRoundClosure)
            {
                throw new InvalidOperationException("The round cannot close early in the current run state.");
            }

            Status = RunStatus.RoundClosurePending;
        }

        public bool PayDebt(Wallet wallet, Money amount)
        {
            return PayDebt(
                wallet,
                new DebtPaymentAllocation(amount, Money.Zero)).Succeeded;
        }

        public bool PayDebtFromProtected(Wallet wallet, Money amount)
        {
            return PayDebt(
                wallet,
                new DebtPaymentAllocation(Money.Zero, amount)).Succeeded;
        }

        public DebtPaymentResult PayDebt(Wallet wallet, DebtPaymentAllocation allocation)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (!CanPayDebt)
            {
                return DebtPaymentResult.Failed(
                    DebtPaymentFailure.PaymentWindowClosed,
                    _debt.Remaining);
            }

            DebtPaymentResult result = _debt.TryPay(wallet, allocation);

            if (result.Succeeded && _debt.IsPaid)
            {
                AdvanceCycleOrCompleteDemo();
            }

            return result;
        }

        public Money CloseRound()
        {
            Money interest = CloseRoundDeferred();

            if (Status == RunStatus.DebtDeadlinePending)
            {
                CommitDebtDeadlineMissed();
            }

            return interest;
        }

        public Money AbandonRound()
        {
            Money interest = AbandonRoundDeferred();

            if (Status == RunStatus.DebtDeadlinePending)
            {
                CommitDebtDeadlineMissed();
            }

            return interest;
        }

        public Money CloseRoundDeferred()
        {
            if (!CanCloseRound)
            {
                throw new InvalidOperationException("The round is not ready to close.");
            }

            Money interest = _debt.ApplyInterest(_rules.InterestPerRound);
            CompleteRoundDeferred();
            return interest;
        }

        public Money AbandonRoundDeferred()
        {
            if (!CanAbandonRound)
            {
                throw new InvalidOperationException("The round cannot be abandoned in the current run state.");
            }

            Money interest = _debt.ApplyInterest(_rules.InterestPerRound);
            CompleteRoundDeferred();
            return interest;
        }

        public Money AbandonRoundFromFundingRequired()
        {
            if (!CanAbandonRoundFromFundingRequired)
            {
                throw new InvalidOperationException("Only an empty active round can be abandoned through FundingRequired.");
            }

            Money interest = _debt.ApplyInterest(_rules.InterestPerRound);
            CompleteRoundDeferred();
            return interest;
        }

        public void StartExtraordinaryRound()
        {
            if (!CanStartExtraordinaryRound)
            {
                throw new InvalidOperationException("An extraordinary round cannot start in the current run state.");
            }

            long nextRoundInstanceId = checked(RoundInstanceId + 1);
            SecondChanceUsed = true;
            IsExtraordinary = true;
            CurrentRoundNumber = _rules.RoundsPerCycle;
            RoundOrdinal = _rules.RoundsPerCycle + 1;
            RoundInstanceId = nextRoundInstanceId;
            _completedHandsInRound = 0;
            Status = RunStatus.Active;
        }

        public void CommitDebtDeadlineMissed()
        {
            if (!CanCommitDebtDeadline)
            {
                throw new InvalidOperationException("There is no pending debt deadline to commit.");
            }

            Status = RunStatus.DebtDeadlineMissed;
        }

        private void AdvanceCycleOrCompleteDemo()
        {
            if (CurrentCycleNumber == _rules.CycleCount)
            {
                Status = RunStatus.DemoCompleted;
                return;
            }

            CurrentCycleNumber++;
            _debt = new DebtAccount(_rules.GetDebtForCycle(CurrentCycleNumber));
            BeginOrdinaryRound(1);
        }

        private void CompleteRoundDeferred()
        {
            if (IsExtraordinary)
            {
                Status = RunStatus.DebtDeadlineMissed;
                return;
            }

            if (CurrentRoundNumber == _rules.RoundsPerCycle)
            {
                Status = RunStatus.DebtDeadlinePending;
                return;
            }

            BeginOrdinaryRound(CurrentRoundNumber + 1);
        }

        private void BeginOrdinaryRound(int roundNumber)
        {
            long nextRoundInstanceId = checked(RoundInstanceId + 1);
            CurrentRoundNumber = roundNumber;
            RoundOrdinal = roundNumber;
            RoundInstanceId = nextRoundInstanceId;
            IsExtraordinary = false;
            _completedHandsInRound = 0;
            Status = RunStatus.Active;
        }
    }
}
