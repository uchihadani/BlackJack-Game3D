using TwentyThree.Domain.Cards;

namespace TwentyThree.Domain.Dealer
{
    public interface IDealerStrategy
    {
        DealerDecision Decide(HandScore score);
    }
}
