using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Randomness;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Randomness
{
    public sealed class DeterministicRandomStreamTests
    {
        private readonly IRandomStreamFactory _factory = new DeterministicRandomStreamFactory();

        [Test]
        public void KnownSeedAndKeyProduceStableSequence()
        {
            int[] expected =
            {
                1192,
                4634,
                5605,
                6723,
                1259,
                9984,
                4369,
                3283,
                1790,
                7659,
                2343,
                139,
                8927,
                6623,
                7062,
                3133
            };
            IRandomStream stream = _factory.Create(123456789, RandomStreamKeys.Ev01Trigger);
            int[] actual = new int[expected.Length];

            for (int index = 0; index < actual.Length; index++)
            {
                actual[index] = stream.NextInt(BasisPoints.Scale);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void SameSeedKeyAndPositionProduceSameValues()
        {
            IRandomStream first = _factory.Create(-982451653, RandomStreamKeys.CardPerception);
            IRandomStream second = _factory.Create(-982451653, RandomStreamKeys.CardPerception);

            for (int index = 0; index < 128; index++)
            {
                int bound = index + 1;
                Assert.That(second.Position, Is.EqualTo(first.Position));
                Assert.That(second.NextInt(bound), Is.EqualTo(first.NextInt(bound)));
            }
        }

        [Test]
        public void DifferentKeysHaveIsolatedSequences()
        {
            IRandomStream trigger = _factory.Create(20260814, RandomStreamKeys.Ev01Trigger);
            IRandomStream truth = _factory.Create(20260814, RandomStreamKeys.Ev01Truth);
            IRandomStream untouchedTrigger = _factory.Create(20260814, RandomStreamKeys.Ev01Trigger);
            int[] triggerValues = new int[32];
            int[] truthValues = new int[32];

            for (int index = 0; index < truthValues.Length; index++)
            {
                truthValues[index] = truth.NextInt(BasisPoints.Scale);
            }

            for (int index = 0; index < triggerValues.Length; index++)
            {
                triggerValues[index] = trigger.NextInt(BasisPoints.Scale);
                Assert.That(triggerValues[index], Is.EqualTo(untouchedTrigger.NextInt(BasisPoints.Scale)));
            }

            CollectionAssert.AreNotEqual(triggerValues, truthValues);
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            IRandomStream first = _factory.Create(20260814, RandomStreamKeys.Ev03Trigger);
            IRandomStream second = _factory.Create(20260815, RandomStreamKeys.Ev03Trigger);
            int equalValueCount = 0;

            for (int index = 0; index < 32; index++)
            {
                if (first.NextInt(BasisPoints.Scale) == second.NextInt(BasisPoints.Scale))
                {
                    equalValueCount++;
                }
            }

            Assert.That(equalValueCount, Is.LessThan(32));
        }

        [Test]
        public void CapturedStateRestoresTheNextValue()
        {
            IRandomStream original = _factory.Create(314159265, RandomStreamKeys.Ev02Value);

            for (int index = 0; index < 47; index++)
            {
                original.NextInt(index + 2);
            }

            RandomStreamState state = original.CaptureState();
            IRandomStream restored = _factory.Restore(state);

            Assert.That(state.Seed, Is.EqualTo(314159265));
            Assert.That(state.Key, Is.EqualTo(RandomStreamKeys.Ev02Value));
            Assert.That(state.Position, Is.EqualTo(47));
            Assert.That(restored.CaptureState(), Is.EqualTo(state));

            for (int index = 0; index < 64; index++)
            {
                int bound = 10000 - index;
                Assert.That(restored.NextInt(bound), Is.EqualTo(original.NextInt(bound)));
            }
        }

        [Test]
        public void BasisPointRollsStayBetweenZeroAndNineThousandNineHundredNinetyNine()
        {
            IRandomStream stream = _factory.Create(int.MinValue, RandomStreamKeys.It05);

            for (int index = 0; index < 100000; index++)
            {
                int roll = stream.NextInt(BasisPoints.Scale);
                Assert.That(roll, Is.InRange(0, BasisPoints.Scale - 1));
            }

            Assert.That(stream.Position, Is.EqualTo(100000));
        }

        [Test]
        public void ExclusiveBoundsAreRespectedAcrossSizes()
        {
            IRandomStream stream = _factory.Create(int.MaxValue, RandomStreamKeys.Ev02Target);

            for (int bound = 1; bound <= 512; bound++)
            {
                for (int draw = 0; draw < 32; draw++)
                {
                    Assert.That(stream.NextInt(bound), Is.InRange(0, bound - 1));
                }
            }

            Assert.That(stream.NextInt(int.MaxValue), Is.InRange(0, int.MaxValue - 1));
        }

        [Test]
        public void InvalidBoundDoesNotAdvanceTheStream()
        {
            IRandomStream stream = _factory.Create(1, RandomStreamKeys.Ev03Trigger);

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextInt(-1));
            Assert.That(stream.Position, Is.Zero);
        }

        [Test]
        public void ExhaustedPositionDoesNotWrap()
        {
            RandomStreamState state = new RandomStreamState(
                1,
                RandomStreamKeys.Sc01Reshuffle,
                long.MaxValue);
            IRandomStream stream = _factory.Restore(state);

            Assert.Throws<InvalidOperationException>(() => stream.NextInt(2));
            Assert.That(stream.Position, Is.EqualTo(long.MaxValue));
        }
    }
}
