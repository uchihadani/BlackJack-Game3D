using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Domain.Cards
{
    public sealed class Hand
    {
        private readonly List<NumericCard> _cards;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyCards;

        public Hand()
        {
            _cards = new List<NumericCard>();
            _readOnlyCards = _cards.AsReadOnly();
        }

        public Hand(IEnumerable<NumericCard> cards)
            : this()
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            foreach (NumericCard card in cards)
            {
                Add(card);
            }
        }

        public IReadOnlyList<NumericCard> Cards => _readOnlyCards;

        public int Count => _cards.Count;

        public void Add(NumericCard card)
        {
            if (_cards.Contains(card))
            {
                throw new InvalidOperationException($"Card {card.Id} is already in the hand.");
            }

            _cards.Add(card);
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
