using System;

namespace TwentyThree.Domain.Psychology
{
    public sealed class PressureChangeResult
    {
        internal PressureChangeResult(
            PressureChanged pressureChange,
            PressureRuptureEvent rupture,
            LucidityChanged ruptureLucidityChange)
        {
            PressureChange = pressureChange ?? throw new ArgumentNullException(nameof(pressureChange));
            Rupture = rupture;
            RuptureLucidityChange = ruptureLucidityChange;
        }

        public PressureChanged PressureChange { get; }

        public bool DidTriggerRupture => Rupture != null;

        public PressureRuptureEvent Rupture { get; }

        public LucidityChanged RuptureLucidityChange { get; }
    }

    public sealed class LucidityMaximumChangeResult
    {
        internal LucidityMaximumChangeResult(
            LucidityMaximumChanged maximumChange,
            LucidityChanged lucidityChange)
        {
            MaximumChange = maximumChange ?? throw new ArgumentNullException(nameof(maximumChange));
            LucidityChange = lucidityChange;
        }

        public LucidityMaximumChanged MaximumChange { get; }

        public bool DidClampLucidity => LucidityChange != null;

        public LucidityChanged LucidityChange { get; }
    }

    public sealed class DealerRelationshipChangeResult
    {
        internal DealerRelationshipChangeResult(
            DealerRelationshipChanged primaryChange,
            DealerRelationshipChanged secondaryChange)
        {
            PrimaryChange = primaryChange ?? throw new ArgumentNullException(nameof(primaryChange));
            SecondaryChange = secondaryChange;
        }

        public DealerRelationshipChanged PrimaryChange { get; }

        public DealerRelationshipChanged SecondaryChange { get; }

        public int ChangeCount => SecondaryChange == null ? 1 : 2;

        public DealerRelationshipChanged GetChange(int index)
        {
            if (index == 0)
            {
                return PrimaryChange;
            }

            if (index == 1 && SecondaryChange != null)
            {
                return SecondaryChange;
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
