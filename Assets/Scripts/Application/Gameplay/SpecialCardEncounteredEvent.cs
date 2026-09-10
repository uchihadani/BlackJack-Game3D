using TwentyThree.Domain.SpecialCards;

namespace TwentyThree.Application.Gameplay
{
    public readonly struct SpecialCardEncounteredEvent
    {
        public SpecialCardEncounteredEvent(
            SpecialCardId id,
            CardRecipient recipient,
            int extractionIndex,
            bool descriptionHidden)
        {
            Id = id;
            Recipient = recipient;
            ExtractionIndex = extractionIndex;
            DescriptionHidden = descriptionHidden;
        }

        public SpecialCardId Id { get; }

        public CardRecipient Recipient { get; }

        public int ExtractionIndex { get; }

        public bool DescriptionHidden { get; }
    }
}
