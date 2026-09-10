using System;
using NUnit.Framework;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Tests.EditMode.Randomness
{
    public sealed class RandomStreamValueTests
    {
        [Test]
        public void DerivedKeysUseStableContextComponents()
        {
            RandomStreamKey key = RandomStreamKeys.It05
                .Derive(4)
                .Derive(2)
                .Derive(1);

            Assert.That(key.Value, Is.EqualTo("IT-05/4/2/1"));
            Assert.That(key, Is.EqualTo(new RandomStreamKey("IT-05/4/2/1")));
        }

        [Test]
        public void KeyComparisonIsOrdinalAndCaseSensitive()
        {
            Assert.That(
                new RandomStreamKey("EV01_TRIGGER"),
                Is.Not.EqualTo(new RandomStreamKey("ev01_trigger")));
        }

        [Test]
        public void EmptyKeysAndNegativeComponentsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new RandomStreamKey(string.Empty));
            Assert.Throws<ArgumentException>(() => new RandomStreamKey("   "));
            Assert.Throws<ArgumentOutOfRangeException>(() => RandomStreamKeys.RoundDeck.Derive(-1));
        }

        [Test]
        public void StateRejectsUndefinedKeysAndNegativePositions()
        {
            Assert.Throws<ArgumentException>(() => new RandomStreamState(1, default, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RandomStreamState(
                1,
                RandomStreamKeys.RoundDeck,
                -1));
        }
    }
}
