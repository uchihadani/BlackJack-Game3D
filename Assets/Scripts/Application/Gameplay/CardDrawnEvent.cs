using TwentyThree.Domain.Cards;

namespace TwentyThree.Application.Gameplay
{
    public readonly struct CardDrawnEvent
    {
        public CardDrawnEvent(
            NumericCard card,
            CardRecipient recipient,
            bool isFaceDown)
        {
            Card = isFaceDown ? (NumericCard?)null : card;
            Recipient = recipient;
            IsFaceDown = isFaceDown;
        }

        public NumericCard? Card { get; }

        public CardRecipient Recipient { get; }

        public bool IsFaceDown { get; }
    }
}
