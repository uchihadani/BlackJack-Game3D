using System;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Application.Gameplay
{
    public sealed class PendingContentDecision
    {
        public PendingContentDecision(
            ContentSourceKind sourceKind,
            string technicalCode,
            string displayName,
            bool canAccept,
            bool canReject,
            Money rejectionCost,
            bool descriptionHidden,
            bool isBlindChoice)
        {
            if (string.IsNullOrWhiteSpace(technicalCode))
            {
                throw new ArgumentException("A technical code is required.", nameof(technicalCode));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A display name is required.", nameof(displayName));
            }

            SourceKind = sourceKind;
            TechnicalCode = technicalCode;
            DisplayName = displayName;
            CanAccept = canAccept;
            CanReject = canReject;
            RejectionCost = rejectionCost;
            DescriptionHidden = descriptionHidden;
            IsBlindChoice = isBlindChoice;
        }

        public ContentSourceKind SourceKind { get; }

        public string TechnicalCode { get; }

        public string DisplayName { get; }

        public bool CanAccept { get; }

        public bool CanReject { get; }

        public Money RejectionCost { get; }

        public bool DescriptionHidden { get; }

        public bool IsBlindChoice { get; }
    }
}
