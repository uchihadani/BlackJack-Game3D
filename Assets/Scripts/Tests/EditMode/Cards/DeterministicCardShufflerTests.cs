using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class DeterministicCardShufflerTests
    {
        private readonly StandardDeckFactory _deckFactory = new StandardDeckFactory();
        private readonly DemoDeckFactory _demoDeckFactory = new DemoDeckFactory();
        private readonly ICardShuffler _shuffler = new DeterministicCardShuffler();
        private readonly IDeckEntryShuffler _entryShuffler = new DeterministicCardShuffler();

        [Test]
        public void KnownSeedProducesStableOrder()
        {
            int[] expectedFirstIds = { 38, 27, 29, 25, 33, 5, 35, 50, 23, 8, 32, 31 };

            int[] actualFirstIds = _shuffler
                .Shuffle(_deckFactory.Create(), 12345, 0)
                .Take(expectedFirstIds.Length)
                .Select(card => card.Id.Value)
                .ToArray();

            CollectionAssert.AreEqual(expectedFirstIds, actualFirstIds);
        }

        [Test]
        public void SameSeedAndStreamProduceSameOrder()
        {
            int[] first = GetOrder(982451653, 0);
            int[] second = GetOrder(982451653, 0);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void DifferentSeedsAndStreamsProduceDifferentOrders()
        {
            int[] original = GetOrder(20260809, 0);
            int[] differentSeed = GetOrder(20260810, 0);
            int[] emergencyStream = GetOrder(20260809, 1);

            CollectionAssert.AreNotEqual(original, differentSeed);
            CollectionAssert.AreNotEqual(original, emergencyStream);
        }

        [Test]
        public void KnownSeedProducesStableMixedDeckOrder()
        {
            int[] expectedFirstIndices =
            {
                30,
                3,
                56,
                42,
                22,
                4,
                21,
                33,
                7,
                5,
                38,
                27,
                9,
                13,
                8,
                40
            };

            int[] actualFirstIndices = _entryShuffler
                .ShuffleEntries(_demoDeckFactory.Create(), 12345, 0)
                .Take(expectedFirstIndices.Length)
                .Select(entry => entry.StableIndex)
                .ToArray();

            CollectionAssert.AreEqual(expectedFirstIndices, actualFirstIndices);
        }

        [Test]
        public void SameSeedAndStreamProduceSameMixedDeckOrder()
        {
            int[] first = GetMixedOrder(982451653, 0);
            int[] second = GetMixedOrder(982451653, 0);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void DifferentStreamProducesDifferentMixedDeckOrder()
        {
            int[] initial = GetMixedOrder(20260909, 0);
            int[] reshuffle = GetMixedOrder(20260909, 1);

            CollectionAssert.AreNotEqual(initial, reshuffle);
        }

        [Test]
        public void MixedShufflePreservesEveryEntryAndDoesNotMutateSource()
        {
            IReadOnlyList<DeckEntry> source = _demoDeckFactory.Create();
            int[] sourceOrder = source.Select(entry => entry.StableIndex).ToArray();

            IReadOnlyList<DeckEntry> shuffled = _entryShuffler.ShuffleEntries(
                source,
                20260909,
                2);

            CollectionAssert.AreEqual(
                sourceOrder,
                source.Select(entry => entry.StableIndex).ToArray());
            CollectionAssert.AreEquivalent(
                sourceOrder,
                shuffled.Select(entry => entry.StableIndex).ToArray());
            Assert.That(shuffled.Count, Is.EqualTo(DemoDeckFactory.TotalCardCount));
            Assert.That(shuffled.Count(entry => entry.IsNumeric), Is.EqualTo(StandardDeckFactory.CardCount));
            Assert.That(shuffled.Count(entry => entry.IsSpecial), Is.EqualTo(DemoDeckFactory.SpecialCardCount));
        }

        [Test]
        public void MixedShuffleValidatesArgumentsWithoutMutatingInput()
        {
            IReadOnlyList<DeckEntry> source = _demoDeckFactory.Create();
            int[] originalOrder = source.Select(entry => entry.StableIndex).ToArray();

            Assert.Throws<System.ArgumentNullException>(() =>
                _entryShuffler.ShuffleEntries(null, 1, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _entryShuffler.ShuffleEntries(source, 1, -1));
            CollectionAssert.AreEqual(
                originalOrder,
                source.Select(entry => entry.StableIndex).ToArray());
        }

        private int[] GetOrder(int seed, int streamIndex)
        {
            return _shuffler
                .Shuffle(_deckFactory.Create(), seed, streamIndex)
                .Select(card => card.Id.Value)
                .ToArray();
        }

        private int[] GetMixedOrder(int seed, int streamIndex)
        {
            return _entryShuffler
                .ShuffleEntries(_demoDeckFactory.Create(), seed, streamIndex)
                .Select(entry => entry.StableIndex)
                .ToArray();
        }
    }
}
