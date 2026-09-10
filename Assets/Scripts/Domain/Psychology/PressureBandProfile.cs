using System;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Psychology
{
    public readonly struct PressureBandProfile
    {
        private PressureBandProfile(
            PressureBand band,
            int minimumPressure,
            int maximumPressure,
            BasisPoints eventProbabilityMultiplier,
            BasisPoints cardDistortionProbability,
            bool isMagnifyingGlassBlocked,
            bool areSpecialDescriptionsHidden)
        {
            Band = band;
            MinimumPressure = minimumPressure;
            MaximumPressure = maximumPressure;
            EventProbabilityMultiplier = eventProbabilityMultiplier;
            CardDistortionProbability = cardDistortionProbability;
            IsMagnifyingGlassBlocked = isMagnifyingGlassBlocked;
            AreSpecialDescriptionsHidden = areSpecialDescriptionsHidden;
        }

        public PressureBand Band { get; }

        public int MinimumPressure { get; }

        public int MaximumPressure { get; }

        public BasisPoints EventProbabilityMultiplier { get; }

        public BasisPoints CardDistortionProbability { get; }

        public bool IsMagnifyingGlassBlocked { get; }

        public bool AreSpecialDescriptionsHidden { get; }

        public static PressureBandProfile FromPressure(int pressure)
        {
            if (pressure < PsychologyState.MinimumPressure ||
                pressure > PsychologyState.MaximumPressure)
            {
                throw new ArgumentOutOfRangeException(nameof(pressure));
            }

            if (pressure <= 24)
            {
                return new PressureBandProfile(
                    PressureBand.Control,
                    0,
                    24,
                    new BasisPoints(10000),
                    BasisPoints.Zero,
                    false,
                    false);
            }

            if (pressure <= 49)
            {
                return new PressureBandProfile(
                    PressureBand.Tension,
                    25,
                    49,
                    new BasisPoints(12500),
                    BasisPoints.Zero,
                    false,
                    false);
            }

            if (pressure <= 69)
            {
                return new PressureBandProfile(
                    PressureBand.Distortion,
                    50,
                    69,
                    new BasisPoints(15000),
                    new BasisPoints(1000),
                    false,
                    false);
            }

            if (pressure <= 84)
            {
                return new PressureBandProfile(
                    PressureBand.Crisis,
                    70,
                    84,
                    new BasisPoints(17500),
                    new BasisPoints(2000),
                    true,
                    false);
            }

            if (pressure <= 99)
            {
                return new PressureBandProfile(
                    PressureBand.Collapse,
                    85,
                    99,
                    new BasisPoints(20000),
                    new BasisPoints(3500),
                    true,
                    true);
            }

            return new PressureBandProfile(
                PressureBand.Rupture,
                100,
                100,
                new BasisPoints(20000),
                new BasisPoints(5000),
                true,
                true);
        }

        public BasisPoints CalculateEffectiveEventProbability(BasisPoints baseProbability)
        {
            if (baseProbability.Value > BasisPoints.Scale)
            {
                throw new ArgumentOutOfRangeException(nameof(baseProbability));
            }

            long numerator = (long)baseProbability.Value * EventProbabilityMultiplier.Value;
            long rounded = numerator / BasisPoints.Scale;
            long remainder = numerator % BasisPoints.Scale;

            if (remainder >= BasisPoints.Scale / 2)
            {
                rounded++;
            }

            return new BasisPoints((int)Math.Min(BasisPoints.Scale, rounded));
        }
    }
}
