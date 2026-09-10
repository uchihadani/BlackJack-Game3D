namespace TwentyThree.Domain.Economy
{
    public enum DebtPaymentFailure
    {
        None = 0,
        AmountMustBePositive = 1,
        AmountOverflow = 2,
        ExceedsRemainingDebt = 3,
        InsufficientAvailableFunds = 4,
        InsufficientProtectedFunds = 5,
        WalletChanged = 6,
        PaymentWindowClosed = 7
    }

    public readonly struct DebtPaymentResult
    {
        private DebtPaymentResult(
            DebtPaymentFailure failure,
            Money paid,
            Money remainingDebt)
        {
            Failure = failure;
            Paid = paid;
            RemainingDebt = remainingDebt;
        }

        public bool Succeeded => Failure == DebtPaymentFailure.None;

        public DebtPaymentFailure Failure { get; }

        public Money Paid { get; }

        public Money RemainingDebt { get; }

        internal static DebtPaymentResult Success(Money paid, Money remainingDebt)
        {
            return new DebtPaymentResult(DebtPaymentFailure.None, paid, remainingDebt);
        }

        internal static DebtPaymentResult Failed(DebtPaymentFailure failure, Money remainingDebt)
        {
            return new DebtPaymentResult(failure, Money.Zero, remainingDebt);
        }
    }
}
