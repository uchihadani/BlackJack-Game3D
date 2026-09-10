using System;

namespace TwentyThree.Domain.Psychology
{
    public sealed class PsychologySnapshot
    {
        public PsychologySnapshot(
            int pressure,
            bool pressureRuptureTriggered,
            int lucidity,
            int lucidityMaximum,
            DealerRelationship dealerRelationship)
        {
            if (pressure < PsychologyState.MinimumPressure ||
                pressure > PsychologyState.MaximumPressure)
            {
                throw new ArgumentOutOfRangeException(nameof(pressure));
            }

            if (lucidityMaximum < PsychologyState.MinimumLucidity ||
                lucidityMaximum > PsychologyState.InitialMaximumLucidity)
            {
                throw new ArgumentOutOfRangeException(nameof(lucidityMaximum));
            }

            if (lucidity < PsychologyState.MinimumLucidity || lucidity > lucidityMaximum)
            {
                throw new ArgumentOutOfRangeException(nameof(lucidity));
            }

            if (pressure == PsychologyState.MaximumPressure && !pressureRuptureTriggered)
            {
                throw new ArgumentException(
                    "Maximum pressure requires the rupture flag to be set.",
                    nameof(pressureRuptureTriggered));
            }

            Pressure = pressure;
            PressureRuptureTriggered = pressureRuptureTriggered;
            Lucidity = lucidity;
            LucidityMaximum = lucidityMaximum;
            DealerRelationship = dealerRelationship;
        }

        public int Pressure { get; }

        public PressureBand PressureBand => PressureBandProfile.FromPressure(Pressure).Band;

        public bool PressureRuptureTriggered { get; }

        public int Lucidity { get; }

        public int LucidityMaximum { get; }

        public LucidityBand LucidityBand => LucidityDistortionProfile.FromLucidity(Lucidity).Band;

        public DealerRelationship DealerRelationship { get; }
    }
}
