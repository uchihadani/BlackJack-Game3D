namespace TwentyThree.Domain.Psychology
{
    public sealed class PressureChanged
    {
        internal PressureChanged(
            int previousValue,
            int requestedDelta,
            int finalValue,
            PressureChangeCause cause,
            PressureBand previousBand,
            PressureBand resultingBand)
        {
            PreviousValue = previousValue;
            RequestedDelta = requestedDelta;
            FinalValue = finalValue;
            Cause = cause;
            PreviousBand = previousBand;
            ResultingBand = resultingBand;
        }

        public int PreviousValue { get; }

        public int RequestedDelta { get; }

        public int AppliedDelta => FinalValue - PreviousValue;

        public int FinalValue { get; }

        public PressureChangeCause Cause { get; }

        public PressureBand PreviousBand { get; }

        public PressureBand ResultingBand { get; }

        public bool ChangedBand => PreviousBand != ResultingBand;

        public bool ChangedValue => PreviousValue != FinalValue;
    }

    public sealed class LucidityChanged
    {
        internal LucidityChanged(
            int previousValue,
            int requestedDelta,
            int finalValue,
            int currentMaximum,
            LucidityChangeCause cause,
            LucidityBand previousBand,
            LucidityBand resultingBand)
        {
            PreviousValue = previousValue;
            RequestedDelta = requestedDelta;
            FinalValue = finalValue;
            CurrentMaximum = currentMaximum;
            Cause = cause;
            PreviousBand = previousBand;
            ResultingBand = resultingBand;
        }

        public int PreviousValue { get; }

        public int RequestedDelta { get; }

        public int AppliedDelta => FinalValue - PreviousValue;

        public int FinalValue { get; }

        public int CurrentMaximum { get; }

        public LucidityChangeCause Cause { get; }

        public LucidityBand PreviousBand { get; }

        public LucidityBand ResultingBand { get; }

        public bool ChangedBand => PreviousBand != ResultingBand;

        public bool ChangedValue => PreviousValue != FinalValue;
    }

    public sealed class LucidityMaximumChanged
    {
        internal LucidityMaximumChanged(
            int previousMaximum,
            int requestedMaximum,
            int finalMaximum,
            int finalLucidity,
            LucidityMaximumReductionCause cause)
        {
            PreviousMaximum = previousMaximum;
            RequestedMaximum = requestedMaximum;
            FinalMaximum = finalMaximum;
            FinalLucidity = finalLucidity;
            Cause = cause;
        }

        public int PreviousMaximum { get; }

        public int RequestedMaximum { get; }

        public int FinalMaximum { get; }

        public int FinalLucidity { get; }

        public LucidityMaximumReductionCause Cause { get; }

        public bool ChangedMaximum => PreviousMaximum != FinalMaximum;
    }

    public sealed class DealerRelationshipChanged
    {
        internal DealerRelationshipChanged(
            DealerRelationshipAxis axis,
            int previousValue,
            int requestedDelta,
            int finalValue,
            DealerRelationshipChangeCause cause)
        {
            Axis = axis;
            PreviousValue = previousValue;
            RequestedDelta = requestedDelta;
            FinalValue = finalValue;
            Cause = cause;
        }

        public DealerRelationshipAxis Axis { get; }

        public int PreviousValue { get; }

        public int RequestedDelta { get; }

        public int AppliedDelta => FinalValue - PreviousValue;

        public int FinalValue { get; }

        public DealerRelationshipChangeCause Cause { get; }

        public bool ChangedValue => PreviousValue != FinalValue;
    }

    public sealed class PressureRuptureEvent
    {
        internal PressureRuptureEvent(int pressure, int lucidityPenalty)
        {
            Pressure = pressure;
            LucidityPenalty = lucidityPenalty;
        }

        public int Pressure { get; }

        public int LucidityPenalty { get; }
    }
}
