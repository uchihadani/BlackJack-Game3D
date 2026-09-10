using System;

namespace TwentyThree.Domain.Psychology
{
    public enum DealerRelationshipAxis
    {
        Respect,
        Greed,
        Dependence,
        Distrust
    }

    public readonly struct DealerRelationship : IEquatable<DealerRelationship>
    {
        public DealerRelationship(int respect, int greed, int dependence, int distrust)
        {
            ValidateAxisValue(respect, nameof(respect));
            ValidateAxisValue(greed, nameof(greed));
            ValidateAxisValue(dependence, nameof(dependence));
            ValidateAxisValue(distrust, nameof(distrust));

            Respect = respect;
            Greed = greed;
            Dependence = dependence;
            Distrust = distrust;
        }

        public static DealerRelationship Empty => new DealerRelationship(0, 0, 0, 0);

        public int Respect { get; }

        public int Greed { get; }

        public int Dependence { get; }

        public int Distrust { get; }

        public int GetValue(DealerRelationshipAxis axis)
        {
            return axis switch
            {
                DealerRelationshipAxis.Respect => Respect,
                DealerRelationshipAxis.Greed => Greed,
                DealerRelationshipAxis.Dependence => Dependence,
                DealerRelationshipAxis.Distrust => Distrust,
                _ => throw new ArgumentOutOfRangeException(nameof(axis))
            };
        }

        public bool Equals(DealerRelationship other)
        {
            return Respect == other.Respect &&
                   Greed == other.Greed &&
                   Dependence == other.Dependence &&
                   Distrust == other.Distrust;
        }

        public override bool Equals(object obj)
        {
            return obj is DealerRelationship other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Respect, Greed, Dependence, Distrust);
        }

        public static bool operator ==(DealerRelationship left, DealerRelationship right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DealerRelationship left, DealerRelationship right)
        {
            return !left.Equals(right);
        }

        private static void ValidateAxisValue(int value, string parameterName)
        {
            if (value < PsychologyState.MinimumRelationshipValue ||
                value > PsychologyState.MaximumRelationshipValue)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
