using System;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Domain.Psychology
{
    public enum CardPerceptionStatus
    {
        SkippedFaceDown,
        SkippedExplicitlyHidden,
        SkippedAlreadyEvaluated,
        SkippedResolutionReveal,
        NotDistorted,
        Distorted
    }

    public sealed class CardPerceptionRecord
    {
        internal CardPerceptionRecord(
            CardPerceptionRequest request,
            CardPerceptionStatus status,
            int? roll,
            CardRank? falseRank,
            int? falseVisualValue,
            RandomStreamState streamStateBefore,
            RandomStreamState streamStateAfter)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Status = status;
            Roll = roll;
            FalseRank = falseRank;
            FalseVisualValue = falseVisualValue;
            StreamStateBefore = streamStateBefore;
            StreamStateAfter = streamStateAfter;
        }

        public CardPerceptionRequest Request { get; }

        public CardId CardId => Request.Card.Id;

        public CardRank RealRank => Request.Card.Rank;

        public CardSuit RealSuit => Request.Card.Suit;

        public int MechanicalVisualValue => Request.MechanicalVisualValue;

        public PressureBandProfile PressureProfile => Request.PressureProfile;

        public LucidityDistortionProfile LucidityProfile => Request.LucidityProfile;

        public CardPerceptionStatus Status { get; }

        public bool WasEvaluated =>
            Status == CardPerceptionStatus.NotDistorted ||
            Status == CardPerceptionStatus.Distorted;

        public bool IsDistorted => Status == CardPerceptionStatus.Distorted;

        public int? Roll { get; }

        public CardRank? FalseRank { get; }

        public int? FalseVisualValue { get; }

        public CardSuit? FalseSuit => IsDistorted ? RealSuit : null;

        public DistortionExpiration? Expiration =>
            IsDistorted ? LucidityProfile.Expiration : null;

        public DistortionSignal? Signal =>
            IsDistorted ? LucidityProfile.Signal : null;

        public TimeSpan? FalseValueDuration =>
            IsDistorted ? LucidityProfile.FalseValueDuration : null;

        public RandomStreamState StreamStateBefore { get; }

        public RandomStreamState StreamStateAfter { get; }
    }
}
