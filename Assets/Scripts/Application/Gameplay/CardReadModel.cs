using TwentyThree.Domain.Cards;

namespace TwentyThree.Application.Gameplay
{
    public readonly struct CardReadModel
    {
        public CardReadModel(
            CardRecipient recipient,
            int position,
            bool isHidden,
            CardId? cardId,
            CardRank? rank,
            CardSuit? suit,
            int? displayedValue,
            bool isDistorted,
            bool hasMechanicalOverride)
        {
            Recipient = recipient;
            Position = position;
            IsHidden = isHidden;
            CardId = cardId;
            Rank = rank;
            Suit = suit;
            DisplayedValue = displayedValue;
            IsDistorted = isDistorted;
            HasMechanicalOverride = hasMechanicalOverride;
        }

        public CardRecipient Recipient { get; }

        public int Position { get; }

        public bool IsHidden { get; }

        public CardId? CardId { get; }

        public CardRank? Rank { get; }

        public CardSuit? Suit { get; }

        public int? DisplayedValue { get; }

        public bool IsDistorted { get; }

        public bool HasMechanicalOverride { get; }
    }
}
