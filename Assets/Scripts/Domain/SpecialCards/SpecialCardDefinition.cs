using System;
using TwentyThree.Domain.Content;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.SpecialCards
{
    public sealed class SpecialCardDefinition
    {
        public SpecialCardDefinition(
            SpecialCardId id,
            string displayName,
            SpecialCardCategory category,
            SpecialCardActivationType activationType,
            ContentTargetKind target,
            Money rejectionCost,
            int appliedPressureDelta,
            ContentDuration duration,
            EffectCategory effectCategory)
        {
            if (!Enum.IsDefined(typeof(SpecialCardId), id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A display name is required.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(SpecialCardCategory), category))
            {
                throw new ArgumentOutOfRangeException(nameof(category));
            }

            if (!Enum.IsDefined(typeof(SpecialCardActivationType), activationType))
            {
                throw new ArgumentOutOfRangeException(nameof(activationType));
            }

            if (!Enum.IsDefined(typeof(ContentTargetKind), target))
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            if (!Enum.IsDefined(typeof(ContentDuration), duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            if (!Enum.IsDefined(typeof(EffectCategory), effectCategory))
            {
                throw new ArgumentOutOfRangeException(nameof(effectCategory));
            }

            Id = id;
            DisplayName = displayName;
            Category = category;
            ActivationType = activationType;
            Target = target;
            RejectionCost = rejectionCost;
            AppliedPressureDelta = appliedPressureDelta;
            Duration = duration;
            EffectCategory = effectCategory;
        }

        public SpecialCardId Id { get; }

        public string DisplayName { get; }

        public SpecialCardCategory Category { get; }

        public SpecialCardActivationType ActivationType { get; }

        public ContentTargetKind Target { get; }

        public Money RejectionCost { get; }

        public int AppliedPressureDelta { get; }

        public ContentDuration Duration { get; }

        public EffectCategory EffectCategory { get; }
    }
}
