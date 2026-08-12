using TwentyThree.Domain.Cards;

namespace TwentyThree.Application.Gameplay
{
    public interface IRoundDeckFactory
    {
        RoundDeck Create(int seed, int minimumCardsToStartHand);
    }
}
