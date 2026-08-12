using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Tests.EditMode.Economy
{
    public sealed class WalletTests
    {
        [Test]
        public void AvailableDebitAndCreditAreAtomic()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));

            Assert.That(wallet.TryDebitAvailable(Money.FromCoins(10)), Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));

            Assert.That(wallet.TryDebitAvailable(Money.FromCoins(41)), Is.False);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(40)));

            wallet.CreditAvailable(Money.FromMinorUnits(920));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(4920)));
        }

        [Test]
        public void ProtectedBalanceIsIndependentFromAvailableBalance()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50), Money.FromCoins(20));

            Assert.That(wallet.TryDebitProtected(Money.FromCoins(5)), Is.True);
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(15)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));

            wallet.CreditProtected(Money.FromCoins(2));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(17)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
        }

        [Test]
        public void FailedProtectedDebitDoesNotMutateEitherBalance()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50), Money.FromCoins(2));

            Assert.That(wallet.TryDebitProtected(Money.FromCoins(3)), Is.False);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.FromCoins(2)));
        }

        [Test]
        public void FailedCreditDoesNotMutateBalance()
        {
            Wallet wallet = new Wallet(Money.FromMinorUnits(long.MaxValue));

            Assert.Throws<OverflowException>(() => wallet.CreditAvailable(Money.FromMinorUnits(1)));
            Assert.That(wallet.Available, Is.EqualTo(Money.FromMinorUnits(long.MaxValue)));
        }
    }
}
