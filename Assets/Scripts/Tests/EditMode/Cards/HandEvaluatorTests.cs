using NUnit.Framework;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Tests.EditMode.Cards
{
    public sealed class HandEvaluatorTests
    {
        private readonly IHandEvaluator _evaluator = new TwentyThreeHandEvaluator();

        [Test]
        public void KeepsAceAtElevenWhenTheHandReachesTwentyThree()
        {
            Hand hand = CreateHand(CardRank.Ace, CardRank.Eight, CardRank.Four);

            HandScore score = _evaluator.Evaluate(hand);

            Assert.That(score.Total, Is.EqualTo(23));
            Assert.That(score.SoftAceCount, Is.EqualTo(1));
            Assert.That(score.IsInitialTwentyThree, Is.True);
        }

        [Test]
        public void ReducesAceToOneToAvoidBust()
        {
            Hand hand = CreateHand(CardRank.Ace, CardRank.Ten, CardRank.Eight);

            HandScore score = _evaluator.Evaluate(hand);

            Assert.That(score.Total, Is.EqualTo(19));
            Assert.That(score.SoftAceCount, Is.Zero);
            Assert.That(score.IsBust, Is.False);
        }

        [Test]
        public void ChoosesTheBestCombinationForMultipleAces()
        {
            Hand hand = new Hand(new[]
            {
                new NumericCard(CardSuit.Hearts, CardRank.Ace),
                new NumericCard(CardSuit.Diamonds, CardRank.Ace),
                new NumericCard(CardSuit.Clubs, CardRank.Ten)
            });

            HandScore score = _evaluator.Evaluate(hand);

            Assert.That(score.Total, Is.EqualTo(22));
            Assert.That(score.SoftAceCount, Is.EqualTo(1));
            Assert.That(score.IsBust, Is.False);
        }

        private static Hand CreateHand(
            CardRank first,
            CardRank second,
            CardRank third)
        {
            return new Hand(new[]
            {
                new NumericCard(CardSuit.Hearts, first),
                new NumericCard(CardSuit.Diamonds, second),
                new NumericCard(CardSuit.Clubs, third)
            });
        }
    }
}
