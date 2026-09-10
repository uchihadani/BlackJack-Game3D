using System;

namespace TwentyThree.Domain.Psychology
{
    public enum DealerRelationshipChangeCause
    {
        AllInConfirmed,
        It01MagnifyingGlassUsed,
        It03SecondChanceActivated,
        Sc05DealerDealAccepted,
        Sc05DealerDealRejected
    }

    public sealed class DealerRelationshipChange
    {
        public DealerRelationshipChange(
            DealerRelationshipChangeCause cause,
            int primaryDelta,
            int? secondaryDelta = null)
        {
            if (!Enum.IsDefined(typeof(DealerRelationshipChangeCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (primaryDelta < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryDelta));
            }

            bool requiresSecondary = cause == DealerRelationshipChangeCause.Sc05DealerDealAccepted;
            if (requiresSecondary != secondaryDelta.HasValue)
            {
                throw new ArgumentException(
                    "The selected cause requires its exact number of relationship deltas.",
                    nameof(secondaryDelta));
            }

            if (secondaryDelta < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondaryDelta));
            }

            Cause = cause;
            PrimaryDelta = primaryDelta;
            SecondaryDelta = secondaryDelta;

            switch (cause)
            {
                case DealerRelationshipChangeCause.AllInConfirmed:
                    PrimaryAxis = DealerRelationshipAxis.Greed;
                    break;
                case DealerRelationshipChangeCause.It01MagnifyingGlassUsed:
                case DealerRelationshipChangeCause.Sc05DealerDealRejected:
                    PrimaryAxis = DealerRelationshipAxis.Distrust;
                    break;
                case DealerRelationshipChangeCause.It03SecondChanceActivated:
                    PrimaryAxis = DealerRelationshipAxis.Dependence;
                    break;
                case DealerRelationshipChangeCause.Sc05DealerDealAccepted:
                    PrimaryAxis = DealerRelationshipAxis.Dependence;
                    SecondaryAxis = DealerRelationshipAxis.Greed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cause));
            }
        }

        public DealerRelationshipChangeCause Cause { get; }

        public DealerRelationshipAxis PrimaryAxis { get; }

        public int PrimaryDelta { get; }

        public DealerRelationshipAxis? SecondaryAxis { get; }

        public int? SecondaryDelta { get; }

        public static DealerRelationshipChange DefaultFor(DealerRelationshipChangeCause cause)
        {
            return cause switch
            {
                DealerRelationshipChangeCause.AllInConfirmed =>
                    new DealerRelationshipChange(cause, 10),
                DealerRelationshipChangeCause.It01MagnifyingGlassUsed =>
                    new DealerRelationshipChange(cause, 10),
                DealerRelationshipChangeCause.It03SecondChanceActivated =>
                    new DealerRelationshipChange(cause, 20),
                DealerRelationshipChangeCause.Sc05DealerDealAccepted =>
                    new DealerRelationshipChange(cause, 25, 15),
                DealerRelationshipChangeCause.Sc05DealerDealRejected =>
                    new DealerRelationshipChange(cause, 10),
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }
    }
}
