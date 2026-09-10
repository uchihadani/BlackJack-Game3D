using System.Collections.Generic;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Domain.Cards
{
    public sealed class DemoDeckFactory
    {
        public const int SpecialCardCount = 5;
        public const int TotalCardCount = StandardDeckFactory.CardCount + SpecialCardCount;

        private readonly StandardDeckFactory _numericDeckFactory;

        public DemoDeckFactory()
        {
            _numericDeckFactory = new StandardDeckFactory();
        }

        public IReadOnlyList<DeckEntry> Create()
        {
            IReadOnlyList<NumericCard> numericCards = _numericDeckFactory.Create();
            DeckEntry[] entries = new DeckEntry[TotalCardCount];
            for (int index = 0; index < numericCards.Count; index++)
            {
                entries[index] = DeckEntry.Numeric(numericCards[index]);
            }

            entries[52] = DeckEntry.Special(SpecialCardId.BeginnersLuck);
            entries[53] = DeckEntry.Special(SpecialCardId.PanicAttack);
            entries[54] = DeckEntry.Special(SpecialCardId.Blackout);
            entries[55] = DeckEntry.Special(SpecialCardId.ThirdEye);
            entries[56] = DeckEntry.Special(SpecialCardId.WeHaveADeal);
            return entries;
        }
    }
}
