using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class MixedRoundDeckTests
    {
        [Test]
        public void StartThresholdCountsOnlyNumericEntries()
        {
            DeckEntry[] entries =
            {
                DeckEntry.Special(SpecialCardId.BeginnersLuck),
                DeckEntry.Special(SpecialCardId.PanicAttack),
                DeckEntry.Special(SpecialCardId.Blackout),
                DeckEntry.Special(SpecialCardId.ThirdEye),
                DeckEntry.Special(SpecialCardId.WeHaveADeal),
                DeckEntry.Numeric(Card(CardRank.Ace)),
                DeckEntry.Numeric(Card(CardRank.Two)),
                DeckEntry.Numeric(Card(CardRank.Three)),
                DeckEntry.Numeric(Card(CardRank.Four)),
                DeckEntry.Numeric(Card(CardRank.Five))
            };

            Assert.That(
                () => new RoundDeck(entries, 1, new IdentityEntryShuffler(), 6),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void DrawsAndRetiresSpecialBeforeNumericReplacement()
        {
            RoundDeck deck = CreateDeck(
                DeckEntry.Special(SpecialCardId.PanicAttack),
                DeckEntry.Numeric(Card(CardRank.Ace)),
                DeckEntry.Numeric(Card(CardRank.Two)),
                DeckEntry.Numeric(Card(CardRank.Three)),
                DeckEntry.Numeric(Card(CardRank.Four)),
                DeckEntry.Numeric(Card(CardRank.Five)),
                DeckEntry.Numeric(Card(CardRank.Six)));

            Assert.That(deck.TryStartHand(), Is.True);
            Assert.That(deck.DrawEntry(out DeckEntry special), Is.EqualTo(DeckDrawStatus.Drawn));
            Assert.That(special.SpecialCardId, Is.EqualTo(SpecialCardId.PanicAttack));
            Assert.That(deck.ActiveCount, Is.Zero);
            Assert.That(deck.RetiredSpecialCards.Single(), Is.EqualTo(SpecialCardId.PanicAttack));

            Assert.That(deck.DrawEntry(out DeckEntry numeric), Is.EqualTo(DeckDrawStatus.Drawn));
            Assert.That(numeric.NumericCard.Rank, Is.EqualTo(CardRank.Ace));
            Assert.That(deck.ActiveCount, Is.EqualTo(1));
            Assert.That(deck.CompleteHand(), Is.True);
            Assert.That(deck.DiscardPile.Single().Rank, Is.EqualTo(CardRank.Ace));
            Assert.That(deck.RetiredSpecialCards, Has.Count.EqualTo(1));
        }

        [Test]
        public void ThirdEyeCandidateSelectionPreservesInterveningSpecials()
        {
            NumericCard first = Card(CardRank.Two);
            NumericCard second = Card(CardRank.Three);
            RoundDeck deck = CreateDeck(
                DeckEntry.Special(SpecialCardId.BeginnersLuck),
                DeckEntry.Numeric(first),
                DeckEntry.Special(SpecialCardId.Blackout),
                DeckEntry.Numeric(second),
                DeckEntry.Numeric(Card(CardRank.Four)),
                DeckEntry.Numeric(Card(CardRank.Five)),
                DeckEntry.Numeric(Card(CardRank.Six)),
                DeckEntry.Numeric(Card(CardRank.Seven)));

            Assert.That(deck.TryStartHand(), Is.True);
            CollectionAssert.AreEqual(
                new[] { first, second },
                deck.GetFirstNumericCandidates(2));

            Assert.That(deck.TryDrawNumericCandidate(1, out NumericCard selected), Is.True);
            Assert.That(selected, Is.EqualTo(second));
            Assert.That(deck.DrawPileEntries[0].SpecialCardId, Is.EqualTo(SpecialCardId.BeginnersLuck));
            Assert.That(deck.DrawPileEntries[1].NumericCard, Is.EqualTo(first));
            Assert.That(deck.DrawPileEntries[2].SpecialCardId, Is.EqualTo(SpecialCardId.Blackout));
        }

        [Test]
        public void RemainingPileReshuffleDoesNotChangeContents()
        {
            RoundDeck deck = CreateDeck(
                DeckEntry.Special(SpecialCardId.BeginnersLuck),
                DeckEntry.Numeric(Card(CardRank.Ace)),
                DeckEntry.Numeric(Card(CardRank.Two)),
                DeckEntry.Numeric(Card(CardRank.Three)),
                DeckEntry.Numeric(Card(CardRank.Four)),
                DeckEntry.Numeric(Card(CardRank.Five)),
                DeckEntry.Numeric(Card(CardRank.Six)));
            int[] before = deck.DrawPileEntries.Select(entry => entry.StableIndex).ToArray();

            Assert.That(deck.ReshuffleRemaining(99), Is.True);

            CollectionAssert.AreEquivalent(
                before,
                deck.DrawPileEntries.Select(entry => entry.StableIndex));
        }

        private static RoundDeck CreateDeck(params DeckEntry[] entries)
        {
            return new RoundDeck(entries, 7, new ReverseEntryShuffler(), 6);
        }

        private static NumericCard Card(CardRank rank)
        {
            return new NumericCard(CardSuit.Hearts, rank);
        }

        private sealed class IdentityEntryShuffler : IDeckEntryShuffler
        {
            public IReadOnlyList<DeckEntry> ShuffleEntries(
                IReadOnlyList<DeckEntry> entries,
                int seed,
                int streamIndex)
            {
                return entries.ToArray();
            }
        }

        private sealed class ReverseEntryShuffler : IDeckEntryShuffler
        {
            public IReadOnlyList<DeckEntry> ShuffleEntries(
                IReadOnlyList<DeckEntry> entries,
                int seed,
                int streamIndex)
            {
                return streamIndex == 0 && seed == 7
                    ? entries.ToArray()
                    : entries.Reverse().ToArray();
            }
        }
    }
}
