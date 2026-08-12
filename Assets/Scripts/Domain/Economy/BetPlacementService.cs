using System;

namespace TwentyThree.Domain.Economy
{
    public sealed class BetPlacementService
    {
        public bool TryPlace(Wallet wallet, Money amount, BetRules rules, out LockedBet bet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            bet = null;

            if (!rules.AllowsStandardBet(amount) || !wallet.TryDebitAvailable(amount))
            {
                return false;
            }

            bet = new LockedBet(amount, false);
            return true;
        }

        public bool TryPlaceAllIn(Wallet wallet, BetRules rules, out LockedBet bet)
        {
            if (wallet == null)
            {
                throw new ArgumentNullException(nameof(wallet));
            }

            bet = null;
            Money amount = wallet.Available;

            if (!rules.AllowsAllIn(amount) || !wallet.TryDebitAvailable(amount))
            {
                return false;
            }

            bet = new LockedBet(amount, true);
            return true;
        }
    }
}
