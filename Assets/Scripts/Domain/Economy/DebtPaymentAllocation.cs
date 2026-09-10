namespace TwentyThree.Domain.Economy
{
    public readonly struct DebtPaymentAllocation
    {
        public DebtPaymentAllocation(Money fromAvailable, Money fromProtected)
        {
            FromAvailable = fromAvailable;
            FromProtected = fromProtected;
        }

        public Money FromAvailable { get; }

        public Money FromProtected { get; }

        public bool TryGetTotal(out Money total)
        {
            if (FromAvailable.MinorUnits > long.MaxValue - FromProtected.MinorUnits)
            {
                total = Money.Zero;
                return false;
            }

            total = Money.FromMinorUnits(FromAvailable.MinorUnits + FromProtected.MinorUnits);
            return true;
        }
    }
}
