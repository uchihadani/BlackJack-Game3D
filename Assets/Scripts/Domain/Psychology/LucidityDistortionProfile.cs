using System;

namespace TwentyThree.Domain.Psychology
{
    public readonly struct LucidityDistortionProfile
    {
        private LucidityDistortionProfile(
            LucidityBand band,
            int minimumLucidity,
            int maximumLucidity,
            DistortionExpiration expiration,
            DistortionSignal signal,
            TimeSpan? falseValueDuration)
        {
            Band = band;
            MinimumLucidity = minimumLucidity;
            MaximumLucidity = maximumLucidity;
            Expiration = expiration;
            Signal = signal;
            FalseValueDuration = falseValueDuration;
        }

        public LucidityBand Band { get; }

        public int MinimumLucidity { get; }

        public int MaximumLucidity { get; }

        public DistortionExpiration Expiration { get; }

        public DistortionSignal Signal { get; }

        public TimeSpan? FalseValueDuration { get; }

        public static LucidityDistortionProfile FromLucidity(int lucidity)
        {
            if (lucidity < PsychologyState.MinimumLucidity ||
                lucidity > PsychologyState.InitialMaximumLucidity)
            {
                throw new ArgumentOutOfRangeException(nameof(lucidity));
            }

            if (lucidity == 0)
            {
                return new LucidityDistortionProfile(
                    LucidityBand.Critical,
                    0,
                    0,
                    DistortionExpiration.HandResolution,
                    DistortionSignal.None,
                    null);
            }

            if (lucidity <= 39)
            {
                return new LucidityDistortionProfile(
                    LucidityBand.Confused,
                    1,
                    39,
                    DistortionExpiration.NextValidPlayerCommand,
                    DistortionSignal.None,
                    null);
            }

            if (lucidity <= 69)
            {
                return new LucidityDistortionProfile(
                    LucidityBand.Unstable,
                    40,
                    69,
                    DistortionExpiration.Timed,
                    DistortionSignal.Subtle,
                    TimeSpan.FromSeconds(2));
            }

            return new LucidityDistortionProfile(
                LucidityBand.Clear,
                70,
                100,
                DistortionExpiration.Timed,
                DistortionSignal.Unmistakable,
                TimeSpan.FromMilliseconds(750));
        }
    }
}
