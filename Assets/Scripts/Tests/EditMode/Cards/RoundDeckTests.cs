using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Infrastructure.Random;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class RoundDeckTests
    {
        private readonly StandardDeckFactory _deckFactory = new StandardDeckFactory();
        private readonly ICardShuffler _shuffler = new DeterministicCardShuffler();

        [Test]
        public void PersistsDrawAndDiscardPilesBetweenHands()
        {
            RoundDeck deck = CreateStandardDeck(731);
            HashSet<CardId> firstHandIds = new HashSet<CardId>();
            CardId[] initialOrder = deck.InitialOrder.Select(card => card.Id).ToArray();

            Assert.That(deck.Seed, Is.EqualTo(731));
            Assert.That(deck.MinimumCardsToStartHand, Is.EqualTo(6));
            Assert.That(deck.CanStartHand, Is.True);
            CollectionAssert.AreEqual(
                initialOrder,
                deck.DrawPile.Select(card => card.Id));
            Assert.That(deck.TryStartHand(), Is.True);

            for (int index = 0; index < 6; index++)
            {
                Assert.That(deck.Draw(out NumericCard card), Is.EqualTo(DeckDrawStatus.Drawn));
                Assert.That(firstHandIds.Add(card.Id), Is.True);
            }

            Assert.That(deck.DrawCount, Is.EqualTo(46));
            Assert.That(deck.ActiveCount, Is.EqualTo(6));
            Assert.That(deck.DiscardCount, Is.Zero);
            CollectionAssert.AreEqual(
                initialOrder.Take(6),
                deck.DrawHistory.Select(card => card.Id));
            Assert.That(deck.CompleteHand(), Is.True);
            Assert.That(deck.DrawCount, Is.EqualTo(46));
            Assert.That(deck.ActiveCount, Is.Zero);
            Assert.That(deck.DiscardCount, Is.EqualTo(6));
            CollectionAssert.AreEquivalent(firstHandIds, deck.DiscardPile.Select(card => card.Id));
        }

        [Test]
        public void NeverRepeatsACardBeforeEmergencyReshuffle()
        {
            RoundDeck deck = CreateStandardDeck(991);
            HashSet<CardId> ids = new HashSet<CardId>();

            Assert.That(deck.TryStartHand(), Is.True);
            for (int index = 0; index < StandardDeckFactory.CardCount; index++)
            {
                Assert.That(deck.Draw(out NumericCard card), Is.EqualTo(DeckDrawStatus.Drawn));
                Assert.That(ids.Add(card.Id), Is.True);
            }

            Assert.That(ids.Count, Is.EqualTo(StandardDeckFactory.CardCount));
            Assert.That(deck.Draw(out _), Is.EqualTo(DeckDrawStatus.Unavailable));
            Assert.That(deck.EmergencyReshuffleCount, Is.Zero);
        }

        [Test]
        public void EmergencyReshuffleUsesOnlyPreviousDiscardsAndRecordsHistory()
        {
            const int seed = 445566;
            RoundDeck deck = CreateStandardDeck(seed);
            HashSet<CardId> previousHandIds = new HashSet<CardId>();

            Assert.That(deck.TryStartHand(), Is.True);
            for (int index = 0; index < 6; index++)
            {
                Assert.That(deck.Draw(out NumericCard card), Is.EqualTo(DeckDrawStatus.Drawn));
                previousHandIds.Add(card.Id);
            }

            Assert.That(deck.CompleteHand(), Is.True);
            Assert.That(deck.TryStartHand(), Is.True);

            HashSet<CardId> activeIds = new HashSet<CardId>();
            for (int index = 0; index < 46; index++)
            {
                Assert.That(deck.Draw(out NumericCard card), Is.EqualTo(DeckDrawStatus.Drawn));
                Assert.That(activeIds.Add(card.Id), Is.True);
                Assert.That(previousHandIds.Contains(card.Id), Is.False);
            }

            Assert.That(deck.Draw(out NumericCard recycledCard), Is.EqualTo(DeckDrawStatus.Drawn));
            Assert.That(previousHandIds.Contains(recycledCard.Id), Is.True);
            Assert.That(activeIds.Add(recycledCard.Id), Is.True);
            Assert.That(deck.ActiveCount, Is.EqualTo(47));
            Assert.That(deck.DiscardCount, Is.Zero);
            Assert.That(deck.DrawCount, Is.EqualTo(5));
            Assert.That(deck.EmergencyReshuffleCount, Is.EqualTo(1));

            DeckReshuffleRecord record = deck.ReshuffleHistory.Single();
            Assert.That(record.RoundSeed, Is.EqualTo(seed));
            Assert.That(record.StreamIndex, Is.EqualTo(1));
            Assert.That(record.RecycledCardCount, Is.EqualTo(6));

            Assert.That(deck.CompleteHand(), Is.True);
            Assert.That(deck.CanStartHand, Is.False);
            Assert.That(deck.DrawCount, Is.EqualTo(5));
            Assert.That(deck.DiscardCount, Is.EqualTo(47));
        }

        [Test]
        public void ReturnsUnavailableWhenNoDrawOrPreviousDiscardExists()
        {
            NumericCard[] sixCards = _deckFactory.Create().Take(6).ToArray();
            RoundDeck deck = new RoundDeck(sixCards, 17, _shuffler);

            Assert.That(deck.Draw(out _), Is.EqualTo(DeckDrawStatus.HandNotActive));
            Assert.That(deck.TryStartHand(), Is.True);

            for (int index = 0; index < sixCards.Length; index++)
            {
                Assert.That(deck.Draw(out _), Is.EqualTo(DeckDrawStatus.Drawn));
            }

            Assert.That(deck.Draw(out _), Is.EqualTo(DeckDrawStatus.Unavailable));
            Assert.That(deck.ActiveCount, Is.EqualTo(6));
            Assert.That(deck.DiscardCount, Is.Zero);
        }

        [Test]
        public void RefusesANewHandWhenDrawPileHasFewerThanSixCards()
        {
            RoundDeck deck = CreateStandardDeck(48);

            Assert.That(deck.TryStartHand(), Is.True);
            for (int index = 0; index < 47; index++)
            {
                Assert.That(deck.Draw(out _), Is.EqualTo(DeckDrawStatus.Drawn));
            }

            Assert.That(deck.CompleteHand(), Is.True);
            Assert.That(deck.DrawCount, Is.EqualTo(5));
            Assert.That(deck.DiscardCount, Is.EqualTo(47));
            Assert.That(deck.CanStartHand, Is.False);
            Assert.That(deck.TryStartHand(), Is.False);
        }

        [Test]
        public void UsesConfiguredMinimumCardsToStartAHand()
        {
            NumericCard[] fiveCards = _deckFactory.Create().Take(5).ToArray();
            RoundDeck deck = new RoundDeck(fiveCards, 90210, _shuffler, 5);

            Assert.That(deck.MinimumCardsToStartHand, Is.EqualTo(5));
            Assert.That(deck.CanStartHand, Is.True);
            Assert.That(deck.TryStartHand(), Is.True);
        }

        [Test]
        public void RejectsAStartThresholdLargerThanTheRealDeck()
        {
            NumericCard[] fiveCards = _deckFactory.Create().Take(5).ToArray();

            Assert.That(
                () => new RoundDeck(fiveCards, 90210, _shuffler, 6),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        private RoundDeck CreateStandardDeck(int seed)
        {
            return new RoundDeck(_deckFactory.Create(), seed, _shuffler);
        }
    }
}
