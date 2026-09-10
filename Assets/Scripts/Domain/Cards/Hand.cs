using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TwentyThree.Domain.Cards
{
    public sealed class Hand
    {
        private readonly List<NumericCard> _cards;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyCards;
        private readonly Dictionary<CardId, int> _valueOverrides;
        private int _totalModifier;

        public Hand()
        {
            _cards = new List<NumericCard>();
            _readOnlyCards = _cards.AsReadOnly();
            _valueOverrides = new Dictionary<CardId, int>();
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

        public int TotalModifier => _totalModifier;

        public void Add(NumericCard card)
        {
            if (_cards.Contains(card))
            {
                throw new InvalidOperationException($"Card {card.Id} is already in the hand.");
            }

            _cards.Add(card);
        }

        public bool TrySetValueOverride(CardId cardId, int value)
        {
            if (value < 1 || value > 10 || !_cards.Exists(card => card.Id == cardId))
            {
                return false;
            }

            _valueOverrides[cardId] = value;
            return true;
        }

        public bool TryGetValueOverride(CardId cardId, out int value)
        {
            return _valueOverrides.TryGetValue(cardId, out value);
        }

        public int GetMechanicalValue(NumericCard card)
        {
            return _valueOverrides.TryGetValue(card.Id, out int value)
                ? value
                : card.Value;
        }

        public bool IsFlexibleAce(NumericCard card)
        {
            return card.IsAce && !_valueOverrides.ContainsKey(card.Id);
        }

        public void SetTotalModifier(int modifier)
        {
            _totalModifier = modifier;
        }

        public void Clear()
        {
            _cards.Clear();
            _valueOverrides.Clear();
            _totalModifier = 0;
        }
    }
}
