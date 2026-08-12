using System.Linq;
using NUnit.Framework;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class StandardDeckFactoryTests
    {
        [Test]
        public void CreatesFiftyTwoUniqueCardsAndThirteenPerSuit()
        {
            var cards = new StandardDeckFactory().Create();

            Assert.That(cards.Count, Is.EqualTo(52));
            Assert.That(cards.Select(card => card.Id).Distinct().Count(), Is.EqualTo(52));

            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
            {
                Assert.That(cards.Count(card => card.Suit == suit), Is.EqualTo(13));
            }

            CollectionAssert.AreEqual(
                Enumerable.Range(0, 52),
                cards.Select(card => card.Id.Value));
        }

        [Test]
        public void AssignsTraditionalValuesToRanks()
        {
            Assert.That(new NumericCard(CardSuit.Hearts, CardRank.Ace).Value, Is.EqualTo(11));

            for (int rank = (int)CardRank.Two; rank <= (int)CardRank.Ten; rank++)
            {
                Assert.That(
                    new NumericCard(CardSuit.Hearts, (CardRank)rank).Value,
                    Is.EqualTo(rank));
            }

            Assert.That(new NumericCard(CardSuit.Hearts, CardRank.Jack).Value, Is.EqualTo(10));
            Assert.That(new NumericCard(CardSuit.Hearts, CardRank.Queen).Value, Is.EqualTo(10));
            Assert.That(new NumericCard(CardSuit.Hearts, CardRank.King).Value, Is.EqualTo(10));
        }
    }
}
