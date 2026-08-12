using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Tests.EditMode.Economy
{
    public sealed class MoneyTests
    {
        [Test]
        public void NormativeConversionsUseOneHundredMinorUnitsPerCoin()
        {
            Assert.That(Money.FromCoins(50).MinorUnits, Is.EqualTo(5000));
            Assert.That(Money.FromCoins(10).MinorUnits, Is.EqualTo(1000));
            Assert.That(Money.FromMinorUnits(920).MinorUnits, Is.EqualTo(920));
            Assert.That(Money.Zero.MinorUnits, Is.Zero);
        }

        [Test]
        public void NegativeAmountsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromCoins(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromMinorUnits(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BasisPoints(-1));
        }

        [Test]
        public void ArithmeticAndComparisonUseMinorUnits()
        {
            Money ten = Money.FromCoins(10);
            Money five = Money.FromCoins(5);

            Assert.That(ten + five, Is.EqualTo(Money.FromCoins(15)));
            Assert.That(ten - five, Is.EqualTo(five));
            Assert.That(ten, Is.GreaterThan(five));
            Assert.That(five, Is.LessThanOrEqualTo(ten));
            Assert.That(Money.FromMinorUnits(500), Is.EqualTo(five));
        }

        [Test]
        public void SubtractionCannotProduceNegativeMoney()
        {
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    Money ignored = Money.FromCoins(5) - Money.FromCoins(6);
                });
        }

        [Test]
        public void PercentageUsesNormativeBasisPointValues()
        {
            Money tenCoins = Money.FromCoins(10);

            Assert.That(
                tenCoins.ApplyPercentage(new BasisPoints(9200)),
                Is.EqualTo(Money.FromMinorUnits(920)));
            Assert.That(
                tenCoins.ApplyPercentage(new BasisPoints(11000)),
                Is.EqualTo(Money.FromCoins(11)));
            Assert.That(
                tenCoins.ApplyPercentage(new BasisPoints(12000)),
                Is.EqualTo(Money.FromCoins(12)));
            Assert.That(
                tenCoins.ApplyPercentage(new BasisPoints(15000)),
                Is.EqualTo(Money.FromCoins(15)));
            Assert.That(
                Money.FromCoins(120).ApplyPercentage(new BasisPoints(1500)),
                Is.EqualTo(Money.FromCoins(18)));
        }

        [Test]
        public void PercentageRoundsHalfUpOnceAtTheMinorUnit()
        {
            Money oneMinorUnit = Money.FromMinorUnits(1);

            Assert.That(
                oneMinorUnit.ApplyPercentage(new BasisPoints(4999)),
                Is.EqualTo(Money.Zero));
            Assert.That(
                oneMinorUnit.ApplyPercentage(new BasisPoints(5000)),
                Is.EqualTo(Money.FromMinorUnits(1)));
            Assert.That(
                Money.FromMinorUnits(3).ApplyPercentage(new BasisPoints(5000)),
                Is.EqualTo(Money.FromMinorUnits(2)));
        }

        [Test]
        public void PercentageAvoidsIntermediateOverflowForRepresentableResult()
        {
            Money amount = Money.FromMinorUnits(long.MaxValue);

            Assert.That(
                amount.ApplyPercentage(new BasisPoints(BasisPoints.Scale)),
                Is.EqualTo(amount));
        }

        [Test]
        public void ArithmeticOverflowIsRejected()
        {
            Money maximum = Money.FromMinorUnits(long.MaxValue);

            Assert.Throws<OverflowException>(
                () =>
                {
                    Money ignored = maximum + Money.FromMinorUnits(1);
                });
            Assert.Throws<OverflowException>(() => Money.FromCoins(long.MaxValue));
        }
    }
}
