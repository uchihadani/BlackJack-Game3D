using System;

namespace TwentyThree.Domain.Psychology
{
    public enum LucidityAdjustmentCause
    {
        It04CigaretteBoxUsed,
        Ev02MiauResolved,
        Ev03DistractedResolved
    }

    public enum LuciditySetCause
    {
        It03SecondChanceActivated
    }

    public enum LucidityMaximumReductionCause
    {
        Sc05DealerDealAccepted
    }

    public enum LucidityChangeCause
    {
        PressureRupture,
        It04CigaretteBoxUsed,
        Ev02MiauResolved,
        Ev03DistractedResolved,
        It03SecondChanceActivated,
        Sc05DealerDealAccepted
    }

    public sealed class LucidityDelta
    {
        public LucidityDelta(LucidityAdjustmentCause cause, int points)
        {
            if (!Enum.IsDefined(typeof(LucidityAdjustmentCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (points > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points));
            }

            Cause = cause;
            Points = points;
        }

        public LucidityAdjustmentCause Cause { get; }

        public int Points { get; }

        public static LucidityDelta DefaultFor(LucidityAdjustmentCause cause)
        {
            return cause switch
            {
                LucidityAdjustmentCause.It04CigaretteBoxUsed => new LucidityDelta(cause, -30),
                LucidityAdjustmentCause.Ev02MiauResolved => new LucidityDelta(cause, -25),
                LucidityAdjustmentCause.Ev03DistractedResolved => new LucidityDelta(cause, -30),
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }
    }

    public sealed class LuciditySetRequest
    {
        public LuciditySetRequest(LuciditySetCause cause, int value)
        {
            if (!Enum.IsDefined(typeof(LuciditySetCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (value < PsychologyState.MinimumLucidity ||
                value > PsychologyState.InitialMaximumLucidity)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Cause = cause;
            Value = value;
        }

        public LuciditySetCause Cause { get; }

        public int Value { get; }

        public static LuciditySetRequest SecondChanceDefault()
        {
            return new LuciditySetRequest(LuciditySetCause.It03SecondChanceActivated, 40);
        }
    }

    public sealed class LucidityMaximumReduction
    {
        public LucidityMaximumReduction(LucidityMaximumReductionCause cause, int maximum)
        {
            if (!Enum.IsDefined(typeof(LucidityMaximumReductionCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (maximum < PsychologyState.MinimumLucidity ||
                maximum > PsychologyState.InitialMaximumLucidity)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            Cause = cause;
            Maximum = maximum;
        }

        public LucidityMaximumReductionCause Cause { get; }

        public int Maximum { get; }

        public static LucidityMaximumReduction SurrenderedEyeDefault()
        {
            return new LucidityMaximumReduction(
                LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                70);
        }
    }
}
