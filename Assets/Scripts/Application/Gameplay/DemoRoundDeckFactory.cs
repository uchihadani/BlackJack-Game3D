using System;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Application.Gameplay
{
    public sealed class DemoRoundDeckFactory : IRoundDeckFactory
    {
        private readonly DemoDeckFactory _deckFactory;
        private readonly IDeckEntryShuffler _shuffler;

        public DemoRoundDeckFactory(
            DemoDeckFactory deckFactory,
            IDeckEntryShuffler shuffler)
        {
            _deckFactory = deckFactory ?? throw new ArgumentNullException(nameof(deckFactory));
            _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
        }

        public RoundDeck Create(int seed, int minimumCardsToStartHand)
        {
            return new RoundDeck(
                _deckFactory.Create(),
                seed,
                _shuffler,
                minimumCardsToStartHand);
        }
    }
}
