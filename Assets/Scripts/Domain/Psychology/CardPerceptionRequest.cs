using System;
using TwentyThree.Domain.Cards;

namespace TwentyThree.Domain.Psychology
{
    public enum CardPerceptionVisibility
    {
        FaceDown,
        Visible,
        ExplicitlyHidden,
        ResolutionReveal
    }

    public sealed class CardPerceptionRequest
    {
        public CardPerceptionRequest(
            NumericCard card,
            CardPerceptionVisibility visibility,
            bool wasAlreadyEvaluated,
            int pressure,
            int lucidity,
            int? mechanicalValueOverride = null)
        {
            if (!Enum.IsDefined(typeof(CardSuit), card.Suit) ||
                !Enum.IsDefined(typeof(CardRank), card.Rank))
            {
                throw new ArgumentException("A valid numeric card is required.", nameof(card));
            }

            if (!Enum.IsDefined(typeof(CardPerceptionVisibility), visibility))
            {
                throw new ArgumentOutOfRangeException(nameof(visibility));
            }

            if (mechanicalValueOverride.HasValue &&
                (mechanicalValueOverride.Value < 1 || mechanicalValueOverride.Value > 10))
            {
                throw new ArgumentOutOfRangeException(nameof(mechanicalValueOverride));
            }

            Card = card;
            Visibility = visibility;
            WasAlreadyEvaluated = wasAlreadyEvaluated;
            MechanicalValueOverride = mechanicalValueOverride;
            MechanicalVisualValue = mechanicalValueOverride ?? GetBaseVisualValue(card.Rank);
            PressureProfile = PressureBandProfile.FromPressure(pressure);
            LucidityProfile = LucidityDistortionProfile.FromLucidity(lucidity);
        }

        public NumericCard Card { get; }

        public CardPerceptionVisibility Visibility { get; }

        public bool WasAlreadyEvaluated { get; }

        public int? MechanicalValueOverride { get; }

        public int MechanicalVisualValue { get; }

        public PressureBandProfile PressureProfile { get; }

        public LucidityDistortionProfile LucidityProfile { get; }

        private static int GetBaseVisualValue(CardRank rank)
        {
            if (rank == CardRank.Ace)
            {
                return 1;
            }

            return rank >= CardRank.Ten ? 10 : (int)rank;
        }
    }
}
