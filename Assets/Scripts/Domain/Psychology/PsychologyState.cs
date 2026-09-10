using System;

namespace TwentyThree.Domain.Psychology
{
    public sealed class PsychologyState
    {
        public const int MinimumPressure = 0;
        public const int MaximumPressure = 100;
        public const int InitialPressure = 0;
        public const int MinimumLucidity = 0;
        public const int InitialMaximumLucidity = 100;
        public const int InitialLucidity = 100;
        public const int MinimumRelationshipValue = 0;
        public const int MaximumRelationshipValue = 100;
        public const int PressureRuptureLucidityPenalty = -25;

        private int _pressure;
        private bool _pressureRuptureTriggered;
        private int _lucidity;
        private int _lucidityMaximum;
        private DealerRelationship _dealerRelationship;

        public PsychologyState()
        {
            _pressure = InitialPressure;
            _lucidityMaximum = InitialMaximumLucidity;
            _lucidity = InitialLucidity;
            _dealerRelationship = DealerRelationship.Empty;
        }

        private PsychologyState(PsychologySnapshot snapshot)
        {
            _pressure = snapshot.Pressure;
            _pressureRuptureTriggered = snapshot.PressureRuptureTriggered;
            _lucidity = snapshot.Lucidity;
            _lucidityMaximum = snapshot.LucidityMaximum;
            _dealerRelationship = snapshot.DealerRelationship;
        }

        public int Pressure => _pressure;

        public PressureBand PressureBand => PressureProfile.Band;

        public PressureBandProfile PressureProfile => PressureBandProfile.FromPressure(_pressure);

        public bool PressureRuptureTriggered => _pressureRuptureTriggered;

        public int Lucidity => _lucidity;

        public int LucidityMaximum => _lucidityMaximum;

        public LucidityBand LucidityBand => LucidityProfile.Band;

        public LucidityDistortionProfile LucidityProfile =>
            LucidityDistortionProfile.FromLucidity(_lucidity);

        public DealerRelationship DealerRelationship => _dealerRelationship;

        public static PsychologyState FromSnapshot(PsychologySnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            return new PsychologyState(snapshot);
        }

        public PsychologySnapshot CreateSnapshot()
        {
            return new PsychologySnapshot(
                _pressure,
                _pressureRuptureTriggered,
                _lucidity,
                _lucidityMaximum,
                _dealerRelationship);
        }

        public PressureChangeResult ApplyPressure(PressureDelta delta)
        {
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }

            int previousPressure = _pressure;
            PressureBand previousBand = PressureBandProfile.FromPressure(previousPressure).Band;
            int finalPressure = ClampWithDelta(
                previousPressure,
                delta.Points,
                MinimumPressure,
                MaximumPressure);
            PressureBand resultingBand = PressureBandProfile.FromPressure(finalPressure).Band;
            bool triggersRupture = !_pressureRuptureTriggered &&
                                   previousPressure < MaximumPressure &&
                                   finalPressure == MaximumPressure;

            PressureChanged pressureChange = new PressureChanged(
                previousPressure,
                delta.Points,
                finalPressure,
                delta.Cause,
                previousBand,
                resultingBand);

            PressureRuptureEvent rupture = null;
            LucidityChanged ruptureLucidityChange = null;
            int finalLucidity = _lucidity;

            if (triggersRupture)
            {
                finalLucidity = ClampWithDelta(
                    _lucidity,
                    PressureRuptureLucidityPenalty,
                    MinimumLucidity,
                    _lucidityMaximum);
                rupture = new PressureRuptureEvent(
                    MaximumPressure,
                    PressureRuptureLucidityPenalty);
                ruptureLucidityChange = CreateLucidityChanged(
                    _lucidity,
                    PressureRuptureLucidityPenalty,
                    finalLucidity,
                    _lucidityMaximum,
                    LucidityChangeCause.PressureRupture);
            }

            PressureChangeResult result = new PressureChangeResult(
                pressureChange,
                rupture,
                ruptureLucidityChange);

            _pressure = finalPressure;
            if (triggersRupture)
            {
                _pressureRuptureTriggered = true;
                _lucidity = finalLucidity;
            }

            return result;
        }

        public LucidityChanged AdjustLucidity(LucidityDelta delta)
        {
            if (delta == null)
            {
                throw new ArgumentNullException(nameof(delta));
            }

            int previousValue = _lucidity;
            int finalValue = ClampWithDelta(
                previousValue,
                delta.Points,
                MinimumLucidity,
                _lucidityMaximum);
            LucidityChanged change = CreateLucidityChanged(
                previousValue,
                delta.Points,
                finalValue,
                _lucidityMaximum,
                ConvertCause(delta.Cause));

            _lucidity = finalValue;
            return change;
        }

        public LucidityChanged SetLucidity(LuciditySetRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            int previousValue = _lucidity;
            int finalValue = Math.Min(request.Value, _lucidityMaximum);
            int requestedDelta = request.Value - previousValue;
            LucidityChanged change = CreateLucidityChanged(
                previousValue,
                requestedDelta,
                finalValue,
                _lucidityMaximum,
                ConvertCause(request.Cause));

            _lucidity = finalValue;
            return change;
        }

        public LucidityMaximumChangeResult ReduceLucidityMaximum(
            LucidityMaximumReduction reduction)
        {
            if (reduction == null)
            {
                throw new ArgumentNullException(nameof(reduction));
            }

            int previousMaximum = _lucidityMaximum;
            int finalMaximum = Math.Min(previousMaximum, reduction.Maximum);
            int previousLucidity = _lucidity;
            int finalLucidity = Math.Min(previousLucidity, finalMaximum);

            LucidityMaximumChanged maximumChange = new LucidityMaximumChanged(
                previousMaximum,
                reduction.Maximum,
                finalMaximum,
                finalLucidity,
                reduction.Cause);

            LucidityChanged lucidityChange = null;
            if (previousLucidity != finalLucidity)
            {
                lucidityChange = CreateLucidityChanged(
                    previousLucidity,
                    finalLucidity - previousLucidity,
                    finalLucidity,
                    finalMaximum,
                    ConvertCause(reduction.Cause));
            }

            LucidityMaximumChangeResult result = new LucidityMaximumChangeResult(
                maximumChange,
                lucidityChange);

            _lucidityMaximum = finalMaximum;
            _lucidity = finalLucidity;
            return result;
        }

        public DealerRelationshipChangeResult ApplyDealerRelationship(
            DealerRelationshipChange change)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            int primaryPrevious = _dealerRelationship.GetValue(change.PrimaryAxis);
            int primaryFinal = ClampWithDelta(
                primaryPrevious,
                change.PrimaryDelta,
                MinimumRelationshipValue,
                MaximumRelationshipValue);

            DealerRelationshipChanged primaryEvent = new DealerRelationshipChanged(
                change.PrimaryAxis,
                primaryPrevious,
                change.PrimaryDelta,
                primaryFinal,
                change.Cause);

            DealerRelationshipChanged secondaryEvent = null;
            int? secondaryFinal = null;

            if (change.SecondaryAxis.HasValue && change.SecondaryDelta.HasValue)
            {
                int secondaryPrevious = _dealerRelationship.GetValue(change.SecondaryAxis.Value);
                secondaryFinal = ClampWithDelta(
                    secondaryPrevious,
                    change.SecondaryDelta.Value,
                    MinimumRelationshipValue,
                    MaximumRelationshipValue);
                secondaryEvent = new DealerRelationshipChanged(
                    change.SecondaryAxis.Value,
                    secondaryPrevious,
                    change.SecondaryDelta.Value,
                    secondaryFinal.Value,
                    change.Cause);
            }

            DealerRelationship finalRelationship = ApplyRelationshipValues(
                _dealerRelationship,
                change.PrimaryAxis,
                primaryFinal,
                change.SecondaryAxis,
                secondaryFinal);
            DealerRelationshipChangeResult result = new DealerRelationshipChangeResult(
                primaryEvent,
                secondaryEvent);

            _dealerRelationship = finalRelationship;
            return result;
        }

        private static DealerRelationship ApplyRelationshipValues(
            DealerRelationship source,
            DealerRelationshipAxis primaryAxis,
            int primaryValue,
            DealerRelationshipAxis? secondaryAxis,
            int? secondaryValue)
        {
            int respect = source.Respect;
            int greed = source.Greed;
            int dependence = source.Dependence;
            int distrust = source.Distrust;

            SetRelationshipAxis(
                primaryAxis,
                primaryValue,
                ref respect,
                ref greed,
                ref dependence,
                ref distrust);

            if (secondaryAxis.HasValue && secondaryValue.HasValue)
            {
                SetRelationshipAxis(
                    secondaryAxis.Value,
                    secondaryValue.Value,
                    ref respect,
                    ref greed,
                    ref dependence,
                    ref distrust);
            }

            return new DealerRelationship(respect, greed, dependence, distrust);
        }

        private static void SetRelationshipAxis(
            DealerRelationshipAxis axis,
            int value,
            ref int respect,
            ref int greed,
            ref int dependence,
            ref int distrust)
        {
            switch (axis)
            {
                case DealerRelationshipAxis.Respect:
                    respect = value;
                    break;
                case DealerRelationshipAxis.Greed:
                    greed = value;
                    break;
                case DealerRelationshipAxis.Dependence:
                    dependence = value;
                    break;
                case DealerRelationshipAxis.Distrust:
                    distrust = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(axis));
            }
        }

        private static LucidityChanged CreateLucidityChanged(
            int previousValue,
            int requestedDelta,
            int finalValue,
            int currentMaximum,
            LucidityChangeCause cause)
        {
            return new LucidityChanged(
                previousValue,
                requestedDelta,
                finalValue,
                currentMaximum,
                cause,
                LucidityDistortionProfile.FromLucidity(previousValue).Band,
                LucidityDistortionProfile.FromLucidity(finalValue).Band);
        }

        private static LucidityChangeCause ConvertCause(LucidityAdjustmentCause cause)
        {
            return cause switch
            {
                LucidityAdjustmentCause.It04CigaretteBoxUsed =>
                    LucidityChangeCause.It04CigaretteBoxUsed,
                LucidityAdjustmentCause.Ev02MiauResolved =>
                    LucidityChangeCause.Ev02MiauResolved,
                LucidityAdjustmentCause.Ev03DistractedResolved =>
                    LucidityChangeCause.Ev03DistractedResolved,
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }

        private static LucidityChangeCause ConvertCause(LuciditySetCause cause)
        {
            return cause switch
            {
                LuciditySetCause.It03SecondChanceActivated =>
                    LucidityChangeCause.It03SecondChanceActivated,
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }

        private static LucidityChangeCause ConvertCause(LucidityMaximumReductionCause cause)
        {
            return cause switch
            {
                LucidityMaximumReductionCause.Sc05DealerDealAccepted =>
                    LucidityChangeCause.Sc05DealerDealAccepted,
                _ => throw new ArgumentOutOfRangeException(nameof(cause))
            };
        }

        private static int ClampWithDelta(int current, int delta, int minimum, int maximum)
        {
            long result = (long)current + delta;
            if (result < minimum)
            {
                return minimum;
            }

            if (result > maximum)
            {
                return maximum;
            }

            return (int)result;
        }
    }
}
