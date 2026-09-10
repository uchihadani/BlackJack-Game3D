using System;

namespace TwentyThree.Domain.Psychology
{
    public enum PressureChangeCause
    {
        VoluntaryHit,
        VoluntaryHitAfterPreviousLoss,
        AllInConfirmed,
        AllInLost,
        Sc02PanicAttackResolved,
        Sc03BlackoutResolved,
        Sc04ThirdEyeResolved,
        Sc05DealerDealAccepted,
        Sc05DealerDealRejectedWithoutFunds,
        It04CigaretteBoxUsed,
        Ev01VoicesBeyondResolved
    }

    public sealed class PressureDelta
    {
        public PressureDelta(PressureChangeCause cause, int points)
        {
            if (!Enum.IsDefined(typeof(PressureChangeCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            bool isReduction = cause == PressureChangeCause.It04CigaretteBoxUsed ||
                               cause == PressureChangeCause.Ev01VoicesBeyondResolved;

            if (isReduction && points > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points));
            }

            if (!isReduction && points < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points));
            }

            Cause = cause;
            Points = points;
        }

        public PressureChangeCause Cause { get; }

        public int Points { get; }

        public static PressureDelta DefaultFor(PressureChangeCause cause)
        {
            return cause switch
            {
                PressureChangeCause.VoluntaryHit => new PressureDelta(cause, 10),
                PressureChangeCause.VoluntaryHitAfterPreviousLoss => new PressureDelta(cause, 15),
                PressureChangeCause.AllInConfirmed => new PressureDelta(cause, 15),
                PressureChangeCause.AllInLost => new PressureDelta(cause, 10),
                PressureChangeCause.Sc02PanicAttackResolved => new PressureDelta(cause, 20),
                PressureChangeCause.Sc03BlackoutResolved => new PressureDelta(cause, 25),
                PressureChangeCause.Sc04ThirdEyeResolved => new PressureDelta(cause, 40),
                PressureChangeCause.Sc05DealerDealAccepted => new PressureDelta(cause, 50),
                PressureChangeCause.Sc05DealerDealRejectedWithoutFunds => new PressureDelta(cause, 16),
                PressureChangeCause.It04CigaretteBoxUsed => new PressureDelta(cause, -15),
                PressureChangeCause.Ev01VoicesBeyondResolved => new PressureDelta(cause, -15),
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }
    }
}
