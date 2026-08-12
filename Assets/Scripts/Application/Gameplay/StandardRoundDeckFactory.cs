using System;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Application.Gameplay
{
    public sealed class StandardRoundDeckFactory : IRoundDeckFactory
    {
        private readonly StandardDeckFactory _cardFactory;
        private readonly ICardShuffler _shuffler;

        public StandardRoundDeckFactory(
            StandardDeckFactory cardFactory,
            ICardShuffler shuffler)
        {
            _cardFactory = cardFactory ?? throw new ArgumentNullException(nameof(cardFactory));
            _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
        }

        public RoundDeck Create(int seed, int minimumCardsToStartHand)
        {
            return new RoundDeck(
                _cardFactory.Create(),
                seed,
                _shuffler,
                minimumCardsToStartHand);
        }
    }
}
