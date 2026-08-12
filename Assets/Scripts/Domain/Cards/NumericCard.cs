using System;

namespace TwentyThree.Domain.Cards
{
    public readonly struct NumericCard : IEquatable<NumericCard>
    {
        public NumericCard(CardSuit suit, CardRank rank)
        {
            if (!Enum.IsDefined(typeof(CardSuit), suit))
            {
                throw new ArgumentOutOfRangeException(nameof(suit));
            }

            if (!Enum.IsDefined(typeof(CardRank), rank))
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            Suit = suit;
            Rank = rank;
            Id = new CardId((int)suit * StandardDeckFactory.RanksPerSuit + (int)rank - 1);
        }

        public CardId Id { get; }

        public CardSuit Suit { get; }

        public CardRank Rank { get; }

        public bool IsAce => Rank == CardRank.Ace;

        public int Value
        {
            get
            {
                if (Rank == CardRank.Ace)
                {
                    return 11;
                }

                return Rank >= CardRank.Jack ? 10 : (int)Rank;
            }
        }

        public bool Equals(NumericCard other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is NumericCard other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }

        public static bool operator ==(NumericCard left, NumericCard right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NumericCard left, NumericCard right)
        {
            return !left.Equals(right);
        }
    }
}
