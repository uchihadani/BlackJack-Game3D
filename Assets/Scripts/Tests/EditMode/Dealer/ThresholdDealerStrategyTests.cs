using NUnit.Framework;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Dealer;

namespace TwentyThree.Tests.EditMode.Dealer
{
    public sealed class ThresholdDealerStrategyTests
    {
        [Test]
        public void HitsBelowConfiguredThreshold()
        {
            IDealerStrategy strategy = new ThresholdDealerStrategy(17);

            DealerDecision decision = strategy.Decide(new HandScore(16, 3, 0));

            Assert.That(decision, Is.EqualTo(DealerDecision.Hit));
        }

        [Test]
        public void StandsAtConfiguredThreshold()
        {
            IDealerStrategy strategy = new ThresholdDealerStrategy(17);

            DealerDecision decision = strategy.Decide(new HandScore(17, 3, 0));

            Assert.That(decision, Is.EqualTo(DealerDecision.Stand));
        }

        [Test]
        public void ReportsBustAboveTwentyThree()
        {
            IDealerStrategy strategy = new ThresholdDealerStrategy(17);

            DealerDecision decision = strategy.Decide(new HandScore(24, 4, 0));

            Assert.That(decision, Is.EqualTo(DealerDecision.Bust));
        }

        [Test]
        public void UsesAlternativeConfiguredThreshold()
        {
            IDealerStrategy strategy = new ThresholdDealerStrategy(19);

            Assert.That(
                strategy.Decide(new HandScore(17, 3, 0)),
                Is.EqualTo(DealerDecision.Hit));
            Assert.That(
                strategy.Decide(new HandScore(19, 3, 0)),
                Is.EqualTo(DealerDecision.Stand));
        }
    }
}
