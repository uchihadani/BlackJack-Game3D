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
            Status = RunStatus.Active;
            _debt = new DebtAccount(_rules.GetDebtForCycle(CurrentCycleNumber));
        }

        public RunStatus Status { get; private set; }

        public int CurrentCycleNumber { get; private set; }

        public int CurrentRoundNumber { get; private set; }

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
            return PayDebt(wallet, amount, false);
        }

        public bool PayDebtFromProtected(Wallet wallet, Money amount)
        {
            return PayDebt(wallet, amount, true);
        }

        public Money CloseRound()
        {
            if (!CanCloseRound)
            {
                throw new InvalidOperationException("The round is not ready to close.");
            }

            Money interest = _debt.ApplyInterest(_rules.InterestPerRound);
            CompleteRound();
            return interest;
        }

        public Money AbandonRound()
        {
            if (!CanAbandonRound)
            {
                throw new InvalidOperationException("The round cannot be abandoned in the current run state.");
            }

            Money interest = _debt.ApplyInterest(_rules.InterestPerRound);
            CompleteRound();
            return interest;
        }

        private bool PayDebt(Wallet wallet, Money amount, bool useProtectedFunds)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            if (!CanPayDebt)
            {
                return false;
            }

            bool paid = useProtectedFunds
                ? _debt.TryPayFromProtected(wallet, amount)
                : _debt.TryPayFromAvailable(wallet, amount);

            if (!paid)
            {
                return false;
            }

            if (_debt.IsPaid)
            {
                AdvanceCycleOrCompleteDemo();
            }

            return true;
        }

        private void AdvanceCycleOrCompleteDemo()
        {
            if (CurrentCycleNumber == _rules.CycleCount)
            {
                Status = RunStatus.DemoCompleted;
                return;
            }

            CurrentCycleNumber++;
            CurrentRoundNumber = 1;
            _completedHandsInRound = 0;
            _debt = new DebtAccount(_rules.GetDebtForCycle(CurrentCycleNumber));
            Status = RunStatus.Active;
        }

        private void CompleteRound()
        {
            if (CurrentRoundNumber == _rules.RoundsPerCycle)
            {
                Status = RunStatus.DebtDeadlineMissed;
                return;
            }

            CurrentRoundNumber++;
            _completedHandsInRound = 0;
            Status = RunStatus.Active;
        }
    }
}
