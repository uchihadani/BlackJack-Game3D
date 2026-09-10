using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Domain.Cards
{
    public sealed class RoundDeck
    {
        public const int DefaultMinimumCardsToStartHand = 6;

        private readonly ICardShuffler _numericShuffler;
        private readonly IDeckEntryShuffler _entryShuffler;
        private Queue<DeckEntry> _drawPile;
        private List<NumericCard> _discardPile;
        private List<NumericCard> _activeCards;
        private List<NumericCard> _initialOrder;
        private List<NumericCard> _drawHistory;
        private List<DeckEntry> _initialEntryOrder;
        private List<DeckEntry> _entryDrawHistory;
        private List<SpecialCardId> _retiredSpecialCards;
        private List<DeckReshuffleRecord> _reshuffleHistory;
        private ReadOnlyCollection<NumericCard> _readOnlyDiscardPile;
        private ReadOnlyCollection<NumericCard> _readOnlyActiveCards;
        private ReadOnlyCollection<NumericCard> _readOnlyInitialOrder;
        private ReadOnlyCollection<NumericCard> _readOnlyDrawHistory;
        private ReadOnlyCollection<DeckEntry> _readOnlyInitialEntryOrder;
        private ReadOnlyCollection<DeckEntry> _readOnlyEntryDrawHistory;
        private ReadOnlyCollection<SpecialCardId> _readOnlyRetiredSpecialCards;
        private ReadOnlyCollection<DeckReshuffleRecord> _readOnlyReshuffleHistory;
        private bool _handInProgress;

        public RoundDeck(
            IEnumerable<NumericCard> cards,
            int seed,
            ICardShuffler shuffler,
            int minimumCardsToStartHand = DefaultMinimumCardsToStartHand)
        {
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            NumericCard[] sourceCards = cards.ToArray();
            ValidateMinimum(minimumCardsToStartHand, sourceCards.Length);
            EnsureUniqueNumericCards(sourceCards, nameof(cards));

            _numericShuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
            Seed = seed;
            MinimumCardsToStartHand = minimumCardsToStartHand;
            InitializeCollections();

            IReadOnlyList<NumericCard> shuffledCards = ShuffleNumericAndValidate(sourceCards, 0);
            foreach (NumericCard card in shuffledCards)
            {
                AddInitialEntry(DeckEntry.Numeric(card));
            }
        }

        public RoundDeck(
            IEnumerable<DeckEntry> entries,
            int seed,
            IDeckEntryShuffler shuffler,
            int minimumCardsToStartHand = DefaultMinimumCardsToStartHand)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            DeckEntry[] sourceEntries = entries.ToArray();
            int numericCount = sourceEntries.Count(entry => entry.IsNumeric);
            ValidateMinimum(minimumCardsToStartHand, numericCount);
            EnsureUniqueEntries(sourceEntries, nameof(entries));

            _entryShuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
            Seed = seed;
            MinimumCardsToStartHand = minimumCardsToStartHand;
            InitializeCollections();

            IReadOnlyList<DeckEntry> shuffledEntries = ShuffleEntriesAndValidate(sourceEntries, 0);
            foreach (DeckEntry entry in shuffledEntries)
            {
                AddInitialEntry(entry);
            }
        }

        public int Seed { get; }

        public int MinimumCardsToStartHand { get; }

        public bool HandInProgress => _handInProgress;

        public bool CanStartHand => !_handInProgress && RemainingNumericCount >= MinimumCardsToStartHand;

        public int DrawCount => _drawPile.Count;

        public int RemainingNumericCount => _drawPile.Count(entry => entry.IsNumeric);

        public int DiscardCount => _discardPile.Count;

        public int ActiveCount => _activeCards.Count;

        public int EmergencyReshuffleCount => _reshuffleHistory.Count;

        public IReadOnlyList<NumericCard> DrawPile => _drawPile
            .Where(entry => entry.IsNumeric)
            .Select(entry => entry.NumericCard)
            .ToArray();

        public IReadOnlyList<DeckEntry> DrawPileEntries => _drawPile.ToArray();

        public IReadOnlyList<NumericCard> DiscardPile => _readOnlyDiscardPile;

        public IReadOnlyList<NumericCard> ActiveCards => _readOnlyActiveCards;

        public IReadOnlyList<NumericCard> InitialOrder => _readOnlyInitialOrder;

        public IReadOnlyList<NumericCard> DrawHistory => _readOnlyDrawHistory;

        public IReadOnlyList<DeckEntry> InitialEntryOrder => _readOnlyInitialEntryOrder;

        public IReadOnlyList<DeckEntry> EntryDrawHistory => _readOnlyEntryDrawHistory;

        public IReadOnlyList<SpecialCardId> RetiredSpecialCards => _readOnlyRetiredSpecialCards;

        public IReadOnlyList<DeckReshuffleRecord> ReshuffleHistory => _readOnlyReshuffleHistory;

        public bool TryStartHand()
        {
            if (!CanStartHand)
            {
                return false;
            }

            _handInProgress = true;
            return true;
        }

        public DeckDrawStatus Draw(out NumericCard card)
        {
            if (!_handInProgress)
            {
                card = default;
                return DeckDrawStatus.HandNotActive;
            }

            if (_drawPile.Count > 0 && _drawPile.Peek().IsSpecial)
            {
                card = default;
                return DeckDrawStatus.Unavailable;
            }

            DeckDrawStatus status = DrawEntry(out DeckEntry entry);
            card = status == DeckDrawStatus.Drawn ? entry.NumericCard : default;
            return status;
        }

        public DeckDrawStatus DrawEntry(out DeckEntry entry)
        {
            if (!_handInProgress)
            {
                entry = default;
                return DeckDrawStatus.HandNotActive;
            }

            if (_drawPile.Count == 0 && !TryEmergencyReshuffle())
            {
                entry = default;
                return DeckDrawStatus.Unavailable;
            }

            entry = _drawPile.Dequeue();
            RecordDraw(entry);
            return DeckDrawStatus.Drawn;
        }

        public IReadOnlyList<NumericCard> GetFirstNumericCandidates(int maximumCount)
        {
            if (maximumCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumCount));
            }

            return _drawPile
                .Where(entry => entry.IsNumeric)
                .Take(maximumCount)
                .Select(entry => entry.NumericCard)
                .ToArray();
        }

        public bool TryDrawNumericCandidate(int candidateIndex, out NumericCard card)
        {
            if (!_handInProgress || candidateIndex < 0)
            {
                card = default;
                return false;
            }

            DeckEntry[] entries = _drawPile.ToArray();
            int numericIndex = -1;
            int entryIndex = -1;
            for (int index = 0; index < entries.Length; index++)
            {
                if (!entries[index].IsNumeric)
                {
                    continue;
                }

                numericIndex++;
                if (numericIndex == candidateIndex)
                {
                    entryIndex = index;
                    break;
                }
            }

            if (entryIndex < 0)
            {
                card = default;
                return false;
            }

            DeckEntry selected = entries[entryIndex];
            _drawPile.Clear();
            for (int index = 0; index < entries.Length; index++)
            {
                if (index != entryIndex)
                {
                    _drawPile.Enqueue(entries[index]);
                }
            }

            RecordDraw(selected);
            card = selected.NumericCard;
            return true;
        }

        public bool ReshuffleRemaining(int independentSeed)
        {
            if (_entryShuffler == null || _drawPile.Count < 2)
            {
                return false;
            }

            DeckEntry[] remaining = _drawPile.ToArray();
            IReadOnlyList<DeckEntry> shuffled = _entryShuffler.ShuffleEntries(
                remaining,
                independentSeed,
                0);
            ValidateShuffledEntries(remaining, shuffled);
            _drawPile.Clear();
            foreach (DeckEntry entry in shuffled)
            {
                _drawPile.Enqueue(entry);
            }

            return true;
        }

        public bool CompleteHand()
        {
            if (!_handInProgress)
            {
                return false;
            }

            _discardPile.AddRange(_activeCards);
            _activeCards.Clear();
            _handInProgress = false;
            return true;
        }

        private void InitializeCollections()
        {
            _drawPile = new Queue<DeckEntry>();
            _discardPile = new List<NumericCard>();
            _activeCards = new List<NumericCard>();
            _initialOrder = new List<NumericCard>();
            _drawHistory = new List<NumericCard>();
            _initialEntryOrder = new List<DeckEntry>();
            _entryDrawHistory = new List<DeckEntry>();
            _retiredSpecialCards = new List<SpecialCardId>();
            _reshuffleHistory = new List<DeckReshuffleRecord>();
            _readOnlyDiscardPile = _discardPile.AsReadOnly();
            _readOnlyActiveCards = _activeCards.AsReadOnly();
            _readOnlyInitialOrder = _initialOrder.AsReadOnly();
            _readOnlyDrawHistory = _drawHistory.AsReadOnly();
            _readOnlyInitialEntryOrder = _initialEntryOrder.AsReadOnly();
            _readOnlyEntryDrawHistory = _entryDrawHistory.AsReadOnly();
            _readOnlyRetiredSpecialCards = _retiredSpecialCards.AsReadOnly();
            _readOnlyReshuffleHistory = _reshuffleHistory.AsReadOnly();
        }

        private void AddInitialEntry(DeckEntry entry)
        {
            _initialEntryOrder.Add(entry);
            if (entry.IsNumeric)
            {
                _initialOrder.Add(entry.NumericCard);
            }

            _drawPile.Enqueue(entry);
        }

        private void RecordDraw(DeckEntry entry)
        {
            _entryDrawHistory.Add(entry);
            if (entry.IsNumeric)
            {
                NumericCard card = entry.NumericCard;
                _activeCards.Add(card);
                _drawHistory.Add(card);
            }
            else
            {
                _retiredSpecialCards.Add(entry.SpecialCardId);
            }
        }

        private bool TryEmergencyReshuffle()
        {
            if (_discardPile.Count == 0)
            {
                return false;
            }

            NumericCard[] recyclableCards = _discardPile.ToArray();
            int streamIndex = _reshuffleHistory.Count + 1;

            _discardPile.Clear();
            if (_entryShuffler != null)
            {
                DeckEntry[] recyclableEntries = recyclableCards
                    .Select(DeckEntry.Numeric)
                    .ToArray();
                IReadOnlyList<DeckEntry> shuffledEntries = ShuffleEntriesAndValidate(
                    recyclableEntries,
                    streamIndex);
                foreach (DeckEntry entry in shuffledEntries)
                {
                    _drawPile.Enqueue(entry);
                }
            }
            else
            {
                IReadOnlyList<NumericCard> shuffledCards = ShuffleNumericAndValidate(
                    recyclableCards,
                    streamIndex);
                foreach (NumericCard card in shuffledCards)
                {
                    _drawPile.Enqueue(DeckEntry.Numeric(card));
                }
            }

            _reshuffleHistory.Add(new DeckReshuffleRecord(
                Seed,
                streamIndex,
                recyclableCards.Length));
            return true;
        }

        private IReadOnlyList<NumericCard> ShuffleNumericAndValidate(
            IReadOnlyList<NumericCard> sourceCards,
            int streamIndex)
        {
            IReadOnlyList<NumericCard> shuffledCards = _numericShuffler.Shuffle(
                sourceCards,
                Seed,
                streamIndex);
            if (shuffledCards == null || shuffledCards.Count != sourceCards.Count)
            {
                throw new InvalidOperationException("The card shuffler returned an invalid card count.");
            }

            EnsureUniqueNumericCards(shuffledCards, nameof(shuffledCards));
            HashSet<CardId> sourceIds = new HashSet<CardId>(sourceCards.Select(card => card.Id));
            if (shuffledCards.Any(card => !sourceIds.Contains(card.Id)))
            {
                throw new InvalidOperationException("The card shuffler changed the deck contents.");
            }

            return shuffledCards;
        }

        private IReadOnlyList<DeckEntry> ShuffleEntriesAndValidate(
            IReadOnlyList<DeckEntry> sourceEntries,
            int streamIndex)
        {
            IReadOnlyList<DeckEntry> shuffledEntries = _entryShuffler.ShuffleEntries(
                sourceEntries,
                Seed,
                streamIndex);
            ValidateShuffledEntries(sourceEntries, shuffledEntries);
            return shuffledEntries;
        }

        private static void ValidateShuffledEntries(
            IReadOnlyList<DeckEntry> sourceEntries,
            IReadOnlyList<DeckEntry> shuffledEntries)
        {
            if (shuffledEntries == null || shuffledEntries.Count != sourceEntries.Count)
            {
                throw new InvalidOperationException("The deck shuffler returned an invalid entry count.");
            }

            EnsureUniqueEntries(shuffledEntries, nameof(shuffledEntries));
            HashSet<int> sourceIds = new HashSet<int>(sourceEntries.Select(entry => entry.StableIndex));
            if (shuffledEntries.Any(entry => !sourceIds.Contains(entry.StableIndex)))
            {
                throw new InvalidOperationException("The deck shuffler changed the deck contents.");
            }
        }

        private static void ValidateMinimum(int minimumCardsToStartHand, int numericCount)
        {
            if (minimumCardsToStartHand < 1 || minimumCardsToStartHand > numericCount)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumCardsToStartHand));
            }
        }

        private static void EnsureUniqueNumericCards(
            IReadOnlyCollection<NumericCard> cards,
            string parameterName)
        {
            HashSet<CardId> ids = new HashSet<CardId>();
            foreach (NumericCard card in cards)
            {
                if (!ids.Add(card.Id))
                {
                    throw new ArgumentException(
                        $"Card {card.Id} appears more than once.",
                        parameterName);
                }
            }
        }

        private static void EnsureUniqueEntries(
            IReadOnlyCollection<DeckEntry> entries,
            string parameterName)
        {
            HashSet<int> ids = new HashSet<int>();
            foreach (DeckEntry entry in entries)
            {
                if (!ids.Add(entry.StableIndex))
                {
                    throw new ArgumentException(
                        $"Deck entry {entry.StableIndex} appears more than once.",
                        parameterName);
                }
            }
        }
    }
}
