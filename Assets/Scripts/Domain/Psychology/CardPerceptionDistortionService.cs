using System;
using TwentyThree.Domain.Cards;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Randomness;

namespace TwentyThree.Domain.Psychology
{
    public sealed class CardPerceptionDistortionService : ICardPerceptionDistortionService
    {
        public CardPerceptionRecord Evaluate(
            CardPerceptionRequest request,
            IRandomStream perceptionStream)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (perceptionStream == null)
            {
                throw new ArgumentNullException(nameof(perceptionStream));
            }

            RandomStreamState stateBefore = perceptionStream.CaptureState();
            CardPerceptionStatus? skippedStatus = GetSkippedStatus(request);
            if (skippedStatus.HasValue)
            {
                return CreateRecord(
                    request,
                    skippedStatus.Value,
                    null,
                    null,
                    null,
                    stateBefore,
                    perceptionStream.CaptureState());
            }

            int roll = perceptionStream.NextInt(BasisPoints.Scale);
            if (roll >= request.PressureProfile.CardDistortionProbability.Value)
            {
                return CreateRecord(
                    request,
                    CardPerceptionStatus.NotDistorted,
                    roll,
                    null,
                    null,
                    stateBefore,
                    perceptionStream.CaptureState());
            }

            int falseVisualValue = SelectFalseVisualValue(
                request.MechanicalVisualValue,
                perceptionStream);
            CardRank falseRank = falseVisualValue == 10
                ? SelectTenValueRank(request.Card.Rank, perceptionStream)
                : (CardRank)falseVisualValue;

            return CreateRecord(
                request,
                CardPerceptionStatus.Distorted,
                roll,
                falseRank,
                falseVisualValue,
                stateBefore,
                perceptionStream.CaptureState());
        }

        private static CardPerceptionStatus? GetSkippedStatus(CardPerceptionRequest request)
        {
            if (request.WasAlreadyEvaluated)
            {
                return CardPerceptionStatus.SkippedAlreadyEvaluated;
            }

            return request.Visibility switch
            {
                CardPerceptionVisibility.FaceDown => CardPerceptionStatus.SkippedFaceDown,
                CardPerceptionVisibility.ExplicitlyHidden =>
                    CardPerceptionStatus.SkippedExplicitlyHidden,
                CardPerceptionVisibility.ResolutionReveal =>
                    CardPerceptionStatus.SkippedResolutionReveal,
                _ => null
            };
        }

        private static int SelectFalseVisualValue(
            int mechanicalVisualValue,
            IRandomStream perceptionStream)
        {
            int selectedValue = perceptionStream.NextInt(9) + 1;
            return selectedValue >= mechanicalVisualValue
                ? selectedValue + 1
                : selectedValue;
        }

        private static CardRank SelectTenValueRank(
            CardRank realRank,
            IRandomStream perceptionStream)
        {
            int excludedIndex = realRank >= CardRank.Ten
                ? (int)realRank - (int)CardRank.Ten
                : -1;
            int selectedIndex = perceptionStream.NextInt(excludedIndex >= 0 ? 3 : 4);

            if (excludedIndex >= 0 && selectedIndex >= excludedIndex)
            {
                selectedIndex++;
            }

            return (CardRank)((int)CardRank.Ten + selectedIndex);
        }

        private static CardPerceptionRecord CreateRecord(
            CardPerceptionRequest request,
            CardPerceptionStatus status,
            int? roll,
            CardRank? falseRank,
            int? falseVisualValue,
            RandomStreamState stateBefore,
            RandomStreamState stateAfter)
        {
            return new CardPerceptionRecord(
                request,
                status,
                roll,
                falseRank,
                falseVisualValue,
                stateBefore,
                stateAfter);
        }
    }
}
