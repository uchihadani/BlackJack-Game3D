using System;
using System.Collections.Generic;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Psychology;
using TwentyThree.Domain.Randomness;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Psychology
{
    public sealed class CardPerceptionDistortionServiceTests
    {
        private readonly ICardPerceptionDistortionService _service =
            new CardPerceptionDistortionService();

        [TestCase(CardPerceptionVisibility.FaceDown, false, CardPerceptionStatus.SkippedFaceDown)]
        [TestCase(CardPerceptionVisibility.ExplicitlyHidden, false, CardPerceptionStatus.SkippedExplicitlyHidden)]
        [TestCase(CardPerceptionVisibility.ResolutionReveal, false, CardPerceptionStatus.SkippedResolutionReveal)]
        [TestCase(CardPerceptionVisibility.Visible, true, CardPerceptionStatus.SkippedAlreadyEvaluated)]
        public void IneligibleCardsDoNotConsumePerceptionRng(
            CardPerceptionVisibility visibility,
            bool wasAlreadyEvaluated,
            CardPerceptionStatus expectedStatus)
        {
            ScriptedRandomStream stream = new ScriptedRandomStream();
            CardPerceptionRequest request = Request(
                CardRank.Seven,
                visibility,
                wasAlreadyEvaluated,
                100,
                100);

            CardPerceptionRecord record = _service.Evaluate(request, stream);

            Assert.That(record.Status, Is.EqualTo(expectedStatus));
            Assert.That(record.WasEvaluated, Is.False);
            Assert.That(record.IsDistorted, Is.False);
            Assert.That(record.Roll, Is.Null);
            Assert.That(record.StreamStateBefore.Position, Is.Zero);
            Assert.That(record.StreamStateAfter.Position, Is.Zero);
            Assert.That(stream.Position, Is.Zero);
            Assert.That(stream.RequestedBounds, Is.Empty);
        }

        [Test]
        public void VisibleCardConsumesTriggerRollEvenWhenBandProbabilityIsZero()
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(0);

            CardPerceptionRecord record = _service.Evaluate(
                Request(CardRank.Seven, CardPerceptionVisibility.Visible, false, 49, 100),
                stream);

            Assert.That(record.Status, Is.EqualTo(CardPerceptionStatus.NotDistorted));
            Assert.That(record.WasEvaluated, Is.True);
            Assert.That(record.Roll, Is.Zero);
            Assert.That(record.PressureProfile.CardDistortionProbability.Value, Is.Zero);
            Assert.That(record.FalseRank, Is.Null);
            Assert.That(record.StreamStateAfter.Position, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 10000 }, stream.RequestedBounds);
        }

        [Test]
        public void RollEqualToThresholdDoesNotCreateDistortion()
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(1000);

            CardPerceptionRecord record = _service.Evaluate(
                Request(CardRank.Seven, CardPerceptionVisibility.Visible, false, 50, 100),
                stream);

            Assert.That(record.Status, Is.EqualTo(CardPerceptionStatus.NotDistorted));
            Assert.That(record.Roll, Is.EqualTo(1000));
            Assert.That(record.StreamStateAfter.Position, Is.EqualTo(1));
        }

        [Test]
        public void FalseValueSelectionCoversOrderedCandidatesAndExcludesMechanicalValue()
        {
            HashSet<int> selectedValues = new HashSet<int>();

            for (int candidateIndex = 0; candidateIndex < 9; candidateIndex++)
            {
                ScriptedRandomStream stream = new ScriptedRandomStream(0, candidateIndex, 0);
                CardPerceptionRecord record = _service.Evaluate(
                    Request(CardRank.Seven, CardPerceptionVisibility.Visible, false, 100, 100),
                    stream);

                Assert.That(record.IsDistorted, Is.True);
                Assert.That(record.FalseVisualValue, Is.Not.EqualTo(7));
                selectedValues.Add(record.FalseVisualValue.Value);
            }

            CollectionAssert.AreEquivalent(
                new[] { 1, 2, 3, 4, 5, 6, 8, 9, 10 },
                selectedValues);
        }

        [Test]
        public void MechanicalOverrideIsExcludedAndRealSuitIsPreserved()
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(0, 8, 2);
            CardPerceptionRequest request = Request(
                CardRank.King,
                CardPerceptionVisibility.Visible,
                false,
                100,
                60,
                7,
                CardSuit.Spades);

            CardPerceptionRecord record = _service.Evaluate(request, stream);

            Assert.That(record.MechanicalVisualValue, Is.EqualTo(7));
            Assert.That(record.FalseVisualValue, Is.EqualTo(10));
            Assert.That(record.FalseRank, Is.EqualTo(CardRank.Queen));
            Assert.That(record.FalseRank, Is.Not.EqualTo(CardRank.King));
            Assert.That(record.RealSuit, Is.EqualTo(CardSuit.Spades));
            Assert.That(record.FalseSuit, Is.EqualTo(CardSuit.Spades));
            Assert.That(record.StreamStateAfter.Position, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] { 10000, 9, 3 }, stream.RequestedBounds);
        }

        [TestCase(CardRank.Ten)]
        [TestCase(CardRank.Jack)]
        [TestCase(CardRank.Queen)]
        [TestCase(CardRank.King)]
        public void TenValueFalseRankNeverRepeatsRealRank(CardRank realRank)
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(0, 8, 0);

            CardPerceptionRecord record = _service.Evaluate(
                Request(
                    realRank,
                    CardPerceptionVisibility.Visible,
                    false,
                    100,
                    100,
                    1),
                stream);

            Assert.That(record.FalseVisualValue, Is.EqualTo(10));
            Assert.That(record.FalseRank, Is.Not.EqualTo(realRank));
            Assert.That(record.FalseRank, Is.InRange(CardRank.Ten, CardRank.King));
            CollectionAssert.AreEqual(new[] { 10000, 9, 3 }, stream.RequestedBounds);
        }

        [Test]
        public void FalseAceHasFixedVisualValueOne()
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(0, 0);

            CardPerceptionRecord record = _service.Evaluate(
                Request(CardRank.Nine, CardPerceptionVisibility.Visible, false, 100, 100),
                stream);

            Assert.That(record.FalseRank, Is.EqualTo(CardRank.Ace));
            Assert.That(record.FalseVisualValue, Is.EqualTo(1));
        }

        [TestCase(100, DistortionExpiration.Timed, DistortionSignal.Unmistakable, 750)]
        [TestCase(60, DistortionExpiration.Timed, DistortionSignal.Subtle, 2000)]
        [TestCase(20, DistortionExpiration.NextValidPlayerCommand, DistortionSignal.None, -1)]
        [TestCase(0, DistortionExpiration.HandResolution, DistortionSignal.None, -1)]
        public void DistortionCapturesLucidityProfileAtCreation(
            int lucidity,
            DistortionExpiration expectedExpiration,
            DistortionSignal expectedSignal,
            int expectedDurationMilliseconds)
        {
            ScriptedRandomStream stream = new ScriptedRandomStream(0, 0);
            CardPerceptionRequest request = Request(
                CardRank.Seven,
                CardPerceptionVisibility.Visible,
                false,
                100,
                lucidity);

            CardPerceptionRecord record = _service.Evaluate(request, stream);

            Assert.That(record.LucidityProfile.Band, Is.EqualTo(
                LucidityDistortionProfile.FromLucidity(lucidity).Band));
            Assert.That(record.Expiration, Is.EqualTo(expectedExpiration));
            Assert.That(record.Signal, Is.EqualTo(expectedSignal));

            if (expectedDurationMilliseconds < 0)
            {
                Assert.That(record.FalseValueDuration, Is.Null);
            }
            else
            {
                Assert.That(
                    record.FalseValueDuration.Value.TotalMilliseconds,
                    Is.EqualTo(expectedDurationMilliseconds));
            }
        }

        [Test]
        public void RecordedStreamStateReproducesRollAndFalseRank()
        {
            IRandomStreamFactory factory = new DeterministicRandomStreamFactory();
            IRandomStream originalStream = factory.Create(1, RandomStreamKeys.CardPerception);
            CardPerceptionRequest request = Request(
                CardRank.King,
                CardPerceptionVisibility.Visible,
                false,
                50,
                60,
                7,
                CardSuit.Diamonds);

            CardPerceptionRecord original = _service.Evaluate(request, originalStream);
            IRandomStream restoredStream = factory.Restore(original.StreamStateBefore);
            CardPerceptionRecord reproduced = _service.Evaluate(request, restoredStream);

            Assert.That(original.IsDistorted, Is.True);
            Assert.That(reproduced.Status, Is.EqualTo(original.Status));
            Assert.That(reproduced.Roll, Is.EqualTo(original.Roll));
            Assert.That(reproduced.FalseVisualValue, Is.EqualTo(original.FalseVisualValue));
            Assert.That(reproduced.FalseRank, Is.EqualTo(original.FalseRank));
            Assert.That(reproduced.StreamStateAfter, Is.EqualTo(original.StreamStateAfter));
        }

        [Test]
        public void InvalidRequestAndNullDependenciesAreRejected()
        {
            ScriptedRandomStream stream = new ScriptedRandomStream();

            Assert.Throws<ArgumentOutOfRangeException>(() => Request(
                CardRank.Seven,
                CardPerceptionVisibility.Visible,
                false,
                100,
                100,
                11));
            Assert.Throws<ArgumentException>(() => new CardPerceptionRequest(
                default,
                CardPerceptionVisibility.Visible,
                false,
                100,
                100));
            Assert.Throws<ArgumentNullException>(() => _service.Evaluate(null, stream));
            Assert.Throws<ArgumentNullException>(() => _service.Evaluate(
                Request(CardRank.Seven, CardPerceptionVisibility.Visible, false, 100, 100),
                null));
        }

        private static CardPerceptionRequest Request(
            CardRank rank,
            CardPerceptionVisibility visibility,
            bool wasAlreadyEvaluated,
            int pressure,
            int lucidity,
            int? mechanicalValueOverride = null,
            CardSuit suit = CardSuit.Hearts)
        {
            return new CardPerceptionRequest(
                new NumericCard(suit, rank),
                visibility,
                wasAlreadyEvaluated,
                pressure,
                lucidity,
                mechanicalValueOverride);
        }

        private sealed class ScriptedRandomStream : IRandomStream
        {
            private readonly Queue<int> _values;
            private readonly List<int> _requestedBounds;

            public ScriptedRandomStream(params int[] values)
            {
                _values = new Queue<int>(values);
                _requestedBounds = new List<int>();
            }

            public int Seed => 77;

            public RandomStreamKey Key => RandomStreamKeys.CardPerception;

            public long Position { get; private set; }

            public IReadOnlyList<int> RequestedBounds => _requestedBounds;

            public int NextInt(int maximumExclusive)
            {
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("No scripted value remains.");
                }

                int value = _values.Dequeue();
                if (value < 0 || value >= maximumExclusive)
                {
                    throw new InvalidOperationException("The scripted value is outside the requested range.");
                }

                _requestedBounds.Add(maximumExclusive);
                Position++;
                return value;
            }

            public RandomStreamState CaptureState()
            {
                return new RandomStreamState(Seed, Key, Position);
            }
        }
    }
}
