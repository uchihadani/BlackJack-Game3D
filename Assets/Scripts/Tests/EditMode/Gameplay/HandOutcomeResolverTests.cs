using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Gameplay;

namespace TwentyThree.Tests.EditMode.Gameplay
{
    public sealed class HandOutcomeResolverTests
    {
        private readonly HandOutcomeResolver _resolver = new HandOutcomeResolver();

        [Test]
        public void PlayerBustLosesBeforeAnyDealerResult()
        {
            HandResolution result = _resolver.Resolve(Score(24, 4), Score(25, 4));

            Assert.That(result.Outcome, Is.EqualTo(HandOutcome.Loss));
        }

        [Test]
        public void InitialTwentyThreeWinsImmediatelyAgainstDealerTwentyThree()
        {
            HandResolution result = _resolver.Resolve(Score(23, 3), Score(23, 3));

            Assert.That(result.Outcome, Is.EqualTo(HandOutcome.InitialTwentyThree));
        }

        [Test]
        public void LaterTwentyThreeWinsNormallyAgainstDealerTwentyThree()
        {
            HandResolution result = _resolver.Resolve(Score(23, 4), Score(23, 3));

            Assert.That(result.Outcome, Is.EqualTo(HandOutcome.NormalWin));
        }

        [TestCase(20, 24, HandOutcome.NormalWin)]
        [TestCase(20, 20, HandOutcome.Draw)]
        [TestCase(21, 20, HandOutcome.NormalWin)]
        [TestCase(19, 20, HandOutcome.Loss)]
        public void ResolvesStandardComparisonOrder(
            int playerTotal,
            int dealerTotal,
            HandOutcome expected)
        {
            HandResolution result = _resolver.Resolve(
                Score(playerTotal, 3),
                Score(dealerTotal, 3));

            Assert.That(result.Outcome, Is.EqualTo(expected));
        }

        [Test]
        public void TechnicalDrawPreservesBothScores()
        {
            HandScore player = Score(18, 3);
            HandScore dealer = Score(16, 3);

            HandResolution result = _resolver.TechnicalDraw(player, dealer);

            Assert.That(result.Outcome, Is.EqualTo(HandOutcome.TechnicalDraw));
            Assert.That(result.PlayerScore, Is.EqualTo(player));
            Assert.That(result.DealerScore, Is.EqualTo(dealer));
        }

        private static HandScore Score(int total, int cardCount)
        {
            return new HandScore(total, cardCount, 0);
        }
    }
}
