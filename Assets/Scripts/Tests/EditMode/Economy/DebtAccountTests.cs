using NUnit.Framework;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Tests.EditMode.Economy
{
    public sealed class DebtAccountTests
    {
        [Test]
        public void AvailablePaymentAtomicallyReducesWalletAndDebt()
        {
            Wallet wallet = new Wallet(Money.FromCoins(100));
            DebtAccount debt = new DebtAccount(Money.FromCoins(200));

            Assert.That(debt.TryPayFromAvailable(wallet, Money.FromCoins(50)), Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(debt.Remaining, Is.EqualTo(Money.FromCoins(150)));
            Assert.That(debt.IsPaid, Is.False);
        }

        [Test]
        public void ProtectedMoneyCanPayDebtWithoutChangingAvailableMoney()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50), Money.FromCoins(20));
            DebtAccount debt = new DebtAccount(Money.FromCoins(200));

            Assert.That(debt.TryPayFromProtected(wallet, Money.FromCoins(20)), Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(wallet.Protected, Is.EqualTo(Money.Zero));
            Assert.That(debt.Remaining, Is.EqualTo(Money.FromCoins(180)));
        }

        [Test]
        public void InvalidPaymentDoesNotMutateWalletOrDebt()
        {
            Wallet wallet = new Wallet(Money.FromCoins(50));
            DebtAccount debt = new DebtAccount(Money.FromCoins(200));

            Assert.That(debt.TryPayFromAvailable(wallet, Money.Zero), Is.False);
            Assert.That(debt.TryPayFromAvailable(wallet, Money.FromCoins(51)), Is.False);
            Assert.That(debt.TryPayFromAvailable(wallet, Money.FromCoins(201)), Is.False);
            Assert.That(wallet.Available, Is.EqualTo(Money.FromCoins(50)));
            Assert.That(debt.Remaining, Is.EqualTo(Money.FromCoins(200)));
        }

        [Test]
        public void ExactPaymentMarksDebtAsPaid()
        {
            Wallet wallet = new Wallet(Money.FromCoins(200));
            DebtAccount debt = new DebtAccount(Money.FromCoins(200));

            Assert.That(debt.TryPayFromAvailable(wallet, Money.FromCoins(200)), Is.True);
            Assert.That(wallet.Available, Is.EqualTo(Money.Zero));
            Assert.That(debt.Remaining, Is.EqualTo(Money.Zero));
            Assert.That(debt.IsPaid, Is.True);
        }

        [Test]
        public void NormativeInterestAppliesToRemainingDebt()
        {
            Wallet wallet = new Wallet(Money.FromCoins(80));
            DebtAccount debt = new DebtAccount(Money.FromCoins(200));
            Assert.That(debt.TryPayFromAvailable(wallet, Money.FromCoins(80)), Is.True);

            Money interest = debt.ApplyInterest(new BasisPoints(1500));

            Assert.That(interest, Is.EqualTo(Money.FromCoins(18)));
            Assert.That(debt.Remaining, Is.EqualTo(Money.FromCoins(138)));
        }

        [Test]
        public void InterestUsesHalfUpRounding()
        {
            DebtAccount debt = new DebtAccount(Money.FromMinorUnits(1));

            Money interest = debt.ApplyInterest(new BasisPoints(5000));

            Assert.That(interest, Is.EqualTo(Money.FromMinorUnits(1)));
            Assert.That(debt.Remaining, Is.EqualTo(Money.FromMinorUnits(2)));
        }

        [Test]
        public void PaidDebtDoesNotAccrueInterest()
        {
            DebtAccount debt = new DebtAccount(Money.Zero);

            Money interest = debt.ApplyInterest(new BasisPoints(1500));

            Assert.That(interest, Is.EqualTo(Money.Zero));
            Assert.That(debt.Remaining, Is.EqualTo(Money.Zero));
        }
    }
}
