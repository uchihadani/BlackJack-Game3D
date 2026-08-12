using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TwentyThree.Domain.Cards
{
    public sealed class RoundDeck
    {
        public const int DefaultMinimumCardsToStartHand = 6;

        private readonly ICardShuffler _shuffler;
        private readonly Queue<NumericCard> _drawPile;
        private readonly List<NumericCard> _discardPile;
        private readonly List<NumericCard> _activeCards;
        private readonly List<NumericCard> _initialOrder;
        private readonly List<NumericCard> _drawHistory;
        private readonly List<DeckReshuffleRecord> _reshuffleHistory;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyDiscardPile;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyActiveCards;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyInitialOrder;
        private readonly ReadOnlyCollection<NumericCard> _readOnlyDrawHistory;
        private readonly ReadOnlyCollection<DeckReshuffleRecord> _readOnlyReshuffleHistory;
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

            if (minimumCardsToStartHand < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumCardsToStartHand));
            }

            NumericCard[] sourceCards = cards.ToArray();
            if (minimumCardsToStartHand > sourceCards.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumCardsToStartHand));
            }

            _shuffler = shuffler ?? throw new ArgumentNullException(nameof(shuffler));
            Seed = seed;
            MinimumCardsToStartHand = minimumCardsToStartHand;
            _drawPile = new Queue<NumericCard>();
            _discardPile = new List<NumericCard>();
            _activeCards = new List<NumericCard>();
            _initialOrder = new List<NumericCard>();
            _drawHistory = new List<NumericCard>();
            _reshuffleHistory = new List<DeckReshuffleRecord>();
            _readOnlyDiscardPile = _discardPile.AsReadOnly();
            _readOnlyActiveCards = _activeCards.AsReadOnly();
            _readOnlyInitialOrder = _initialOrder.AsReadOnly();
            _readOnlyDrawHistory = _drawHistory.AsReadOnly();
            _readOnlyReshuffleHistory = _reshuffleHistory.AsReadOnly();

            EnsureUniqueCards(sourceCards, nameof(cards));
            IReadOnlyList<NumericCard> shuffledCards = ShuffleAndValidate(sourceCards, 0);
            foreach (NumericCard card in shuffledCards)
            {
                _initialOrder.Add(card);
                _drawPile.Enqueue(card);
            }
        }

        public int Seed { get; }

        public int MinimumCardsToStartHand { get; }

        public bool HandInProgress => _handInProgress;

        public bool CanStartHand => !_handInProgress && _drawPile.Count >= MinimumCardsToStartHand;

        public int DrawCount => _drawPile.Count;

        public int DiscardCount => _discardPile.Count;

        public int ActiveCount => _activeCards.Count;

        public int EmergencyReshuffleCount => _reshuffleHistory.Count;

        public IReadOnlyList<NumericCard> DrawPile => _drawPile.ToArray();

        public IReadOnlyList<NumericCard> DiscardPile => _readOnlyDiscardPile;

        public IReadOnlyList<NumericCard> ActiveCards => _readOnlyActiveCards;

        public IReadOnlyList<NumericCard> InitialOrder => _readOnlyInitialOrder;

        public IReadOnlyList<NumericCard> DrawHistory => _readOnlyDrawHistory;

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

            if (_drawPile.Count == 0 && !TryEmergencyReshuffle())
            {
                card = default;
                return DeckDrawStatus.Unavailable;
            }

            card = _drawPile.Dequeue();
            _activeCards.Add(card);
            _drawHistory.Add(card);
            return DeckDrawStatus.Drawn;
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

        private bool TryEmergencyReshuffle()
        {
            if (_discardPile.Count == 0)
            {
                return false;
            }

            NumericCard[] recyclableCards = _discardPile.ToArray();
            int streamIndex = _reshuffleHistory.Count + 1;
            IReadOnlyList<NumericCard> shuffledCards = ShuffleAndValidate(
                recyclableCards,
                streamIndex);

            _discardPile.Clear();
            foreach (NumericCard card in shuffledCards)
            {
                _drawPile.Enqueue(card);
            }

            _reshuffleHistory.Add(new DeckReshuffleRecord(
                Seed,
                streamIndex,
                recyclableCards.Length));
            return true;
        }

        private IReadOnlyList<NumericCard> ShuffleAndValidate(
            IReadOnlyList<NumericCard> sourceCards,
            int streamIndex)
        {
            IReadOnlyList<NumericCard> shuffledCards = _shuffler.Shuffle(
                sourceCards,
                Seed,
                streamIndex);
            if (shuffledCards == null || shuffledCards.Count != sourceCards.Count)
            {
                throw new InvalidOperationException("The card shuffler returned an invalid card count.");
            }

            EnsureUniqueCards(shuffledCards, nameof(shuffledCards));
            HashSet<CardId> sourceIds = new HashSet<CardId>(sourceCards.Select(card => card.Id));
            if (shuffledCards.Any(card => !sourceIds.Contains(card.Id)))
            {
                throw new InvalidOperationException("The card shuffler changed the deck contents.");
            }

            return shuffledCards;
        }

        private static void EnsureUniqueCards(
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
    }
}
