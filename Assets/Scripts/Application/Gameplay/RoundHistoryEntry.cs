using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay
{
    public sealed class RoundHistoryEntry
    {
        private readonly ReadOnlyCollection<NumericCard> _initialOrder;
        private readonly ReadOnlyCollection<NumericCard> _drawHistory;
        private readonly ReadOnlyCollection<DeckReshuffleRecord> _reshuffles;
        private readonly ReadOnlyCollection<DeckEntry> _initialEntryOrder;
        private readonly ReadOnlyCollection<DeckEntry> _entryDrawHistory;
        private readonly ReadOnlyCollection<SpecialCardId> _retiredSpecialCards;

        public RoundHistoryEntry(RoundDeck deck)
        {
            if (deck == null)
            {
                throw new ArgumentNullException(nameof(deck));
            }

            Seed = deck.Seed;
            _initialOrder = Array.AsReadOnly(Copy(deck.InitialOrder));
            _drawHistory = Array.AsReadOnly(Copy(deck.DrawHistory));
            _reshuffles = Array.AsReadOnly(Copy(deck.ReshuffleHistory));
            _initialEntryOrder = Array.AsReadOnly(Copy(deck.InitialEntryOrder));
            _entryDrawHistory = Array.AsReadOnly(Copy(deck.EntryDrawHistory));
            _retiredSpecialCards = Array.AsReadOnly(Copy(deck.RetiredSpecialCards));
        }

        public int Seed { get; }

        public IReadOnlyList<NumericCard> InitialOrder => _initialOrder;

        public IReadOnlyList<NumericCard> DrawHistory => _drawHistory;

        public IReadOnlyList<DeckReshuffleRecord> Reshuffles => _reshuffles;

        public IReadOnlyList<DeckEntry> InitialEntryOrder => _initialEntryOrder;

        public IReadOnlyList<DeckEntry> EntryDrawHistory => _entryDrawHistory;

        public IReadOnlyList<SpecialCardId> RetiredSpecialCards => _retiredSpecialCards;

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            T[] result = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }
}
