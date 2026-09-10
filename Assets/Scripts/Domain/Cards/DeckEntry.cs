using System;
using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Domain.Cards
{
    public readonly struct DeckEntry : IEquatable<DeckEntry>
    {
        private readonly NumericCard _numericCard;
        private readonly SpecialCardId _specialCardId;

        private DeckEntry(NumericCard numericCard)
        {
            Kind = DeckEntryKind.Numeric;
            _numericCard = numericCard;
            _specialCardId = default;
        }

        private DeckEntry(SpecialCardId specialCardId)
        {
            if (!Enum.IsDefined(typeof(SpecialCardId), specialCardId))
            {
                throw new ArgumentOutOfRangeException(nameof(specialCardId));
            }

            Kind = DeckEntryKind.Special;
            _numericCard = default;
            _specialCardId = specialCardId;
        }

        public DeckEntryKind Kind { get; }

        public bool IsNumeric => Kind == DeckEntryKind.Numeric;

        public bool IsSpecial => Kind == DeckEntryKind.Special;

        public int StableIndex => IsNumeric
            ? _numericCard.Id.Value
            : StandardDeckFactory.CardCount + (int)_specialCardId - 1;

        public NumericCard NumericCard
        {
            get
            {
                if (!IsNumeric)
                {
                    throw new InvalidOperationException("The deck entry is not numeric.");
                }

                return _numericCard;
            }
        }

        public SpecialCardId SpecialCardId
        {
            get
            {
                if (!IsSpecial)
                {
                    throw new InvalidOperationException("The deck entry is not special.");
                }

                return _specialCardId;
            }
        }

        public static DeckEntry Numeric(NumericCard card)
        {
            return new DeckEntry(card);
        }

        public static DeckEntry Special(SpecialCardId id)
        {
            return new DeckEntry(id);
        }

        public bool Equals(DeckEntry other)
        {
            return Kind == other.Kind && StableIndex == other.StableIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is DeckEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Kind, StableIndex);
        }

        public override string ToString()
        {
            return IsNumeric ? _numericCard.ToString() : _specialCardId.ToString();
        }

        public static bool operator ==(DeckEntry left, DeckEntry right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DeckEntry left, DeckEntry right)
        {
            return !left.Equals(right);
        }
    }
}
