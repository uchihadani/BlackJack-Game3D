using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class DeterministicCardShufflerTests
    {
        private readonly StandardDeckFactory _deckFactory = new StandardDeckFactory();
        private readonly ICardShuffler _shuffler = new DeterministicCardShuffler();

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

        private int[] GetOrder(int seed, int streamIndex)
        {
            return _shuffler
                .Shuffle(_deckFactory.Create(), seed, streamIndex)
                .Select(card => card.Id.Value)
                .ToArray();
        }
    }
}
