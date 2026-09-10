using System;
using NUnit.Framework;
using TwentyThree.Domain.Economy;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Tests.EditMode.Psychology
{
    public sealed class PressureTests
    {
        [TestCase(0, PressureBand.Control, 0, 24, 10000, 0, false, false)]
        [TestCase(24, PressureBand.Control, 0, 24, 10000, 0, false, false)]
        [TestCase(25, PressureBand.Tension, 25, 49, 12500, 0, false, false)]
        [TestCase(49, PressureBand.Tension, 25, 49, 12500, 0, false, false)]
        [TestCase(50, PressureBand.Distortion, 50, 69, 15000, 1000, false, false)]
        [TestCase(69, PressureBand.Distortion, 50, 69, 15000, 1000, false, false)]
        [TestCase(70, PressureBand.Crisis, 70, 84, 17500, 2000, true, false)]
        [TestCase(84, PressureBand.Crisis, 70, 84, 17500, 2000, true, false)]
        [TestCase(85, PressureBand.Collapse, 85, 99, 20000, 3500, true, true)]
        [TestCase(99, PressureBand.Collapse, 85, 99, 20000, 3500, true, true)]
        [TestCase(100, PressureBand.Rupture, 100, 100, 20000, 5000, true, true)]
        public void EveryPressureBoundaryHasItsExactProfile(
            int pressure,
            PressureBand expectedBand,
            int expectedMinimum,
            int expectedMaximum,
            int expectedMultiplier,
            int expectedDistortion,
            bool expectedMagnifyingGlassBlock,
            bool expectedDescriptionHiding)
        {
            PressureBandProfile profile = PressureBandProfile.FromPressure(pressure);

            Assert.That(profile.Band, Is.EqualTo(expectedBand));
            Assert.That(profile.MinimumPressure, Is.EqualTo(expectedMinimum));
            Assert.That(profile.MaximumPressure, Is.EqualTo(expectedMaximum));
            Assert.That(profile.EventProbabilityMultiplier.Value, Is.EqualTo(expectedMultiplier));
            Assert.That(profile.CardDistortionProbability.Value, Is.EqualTo(expectedDistortion));
            Assert.That(profile.IsMagnifyingGlassBlocked, Is.EqualTo(expectedMagnifyingGlassBlock));
            Assert.That(profile.AreSpecialDescriptionsHidden, Is.EqualTo(expectedDescriptionHiding));
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void PressureProfilesRejectValuesOutsideTheRunLimits(int pressure)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PressureBandProfile.FromPressure(pressure));
        }

        [TestCase(0, 1500, 1500)]
        [TestCase(25, 1500, 1875)]
        [TestCase(50, 1500, 2250)]
        [TestCase(70, 1500, 2625)]
        [TestCase(85, 1500, 3000)]
        [TestCase(100, 1500, 3000)]
        [TestCase(25, 2, 3)]
        [TestCase(85, 9999, 10000)]
        public void EventProbabilityUsesExactMultiplierHalfUpRoundingAndCap(
            int pressure,
            int baseProbability,
            int expectedProbability)
        {
            BasisPoints result = PressureBandProfile
                .FromPressure(pressure)
                .CalculateEffectiveEventProbability(new BasisPoints(baseProbability));

            Assert.That(result.Value, Is.EqualTo(expectedProbability));
        }

        [Test]
        public void EventProbabilityRejectsValuesAboveOneHundredPercent()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => PressureBandProfile
                    .FromPressure(0)
                    .CalculateEffectiveEventProbability(new BasisPoints(10001)));
        }

        [TestCase(PressureChangeCause.VoluntaryHit, 10)]
        [TestCase(PressureChangeCause.VoluntaryHitAfterPreviousLoss, 15)]
        [TestCase(PressureChangeCause.AllInConfirmed, 15)]
        [TestCase(PressureChangeCause.AllInLost, 10)]
        [TestCase(PressureChangeCause.Sc02PanicAttackResolved, 20)]
        [TestCase(PressureChangeCause.Sc03BlackoutResolved, 25)]
        [TestCase(PressureChangeCause.Sc04ThirdEyeResolved, 40)]
        [TestCase(PressureChangeCause.Sc05DealerDealAccepted, 50)]
        [TestCase(PressureChangeCause.Sc05DealerDealRejectedWithoutFunds, 16)]
        [TestCase(PressureChangeCause.It04CigaretteBoxUsed, -15)]
        [TestCase(PressureChangeCause.Ev01VoicesBeyondResolved, -15)]
        public void DefaultPressureDeltasMatchTheTechnicalSheet(
            PressureChangeCause cause,
            int expectedPoints)
        {
            PressureDelta delta = PressureDelta.DefaultFor(cause);

            Assert.That(delta.Cause, Is.EqualTo(cause));
            Assert.That(delta.Points, Is.EqualTo(expectedPoints));
        }

        [Test]
        public void PressureDeltaRejectsInvalidCausesAndWrongDirections()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PressureDelta((PressureChangeCause)999, 10));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PressureDelta(PressureChangeCause.VoluntaryHit, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PressureDelta(PressureChangeCause.It04CigaretteBoxUsed, 1));
        }

        [Test]
        public void PressureChangesClampAndExposeRequestedAppliedAndBandTransitionData()
        {
            PsychologyState state = new PsychologyState();

            PressureChangeResult first = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.Sc05DealerDealAccepted, 49));
            PressureChangeResult second = state.ApplyPressure(
                PressureDelta.DefaultFor(PressureChangeCause.VoluntaryHit));
            PressureChangeResult third = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.Sc04ThirdEyeResolved, int.MaxValue));

            Assert.That(first.PressureChange.PreviousValue, Is.Zero);
            Assert.That(first.PressureChange.FinalValue, Is.EqualTo(49));
            Assert.That(first.PressureChange.ResultingBand, Is.EqualTo(PressureBand.Tension));
            Assert.That(second.PressureChange.PreviousValue, Is.EqualTo(49));
            Assert.That(second.PressureChange.RequestedDelta, Is.EqualTo(10));
            Assert.That(second.PressureChange.AppliedDelta, Is.EqualTo(10));
            Assert.That(second.PressureChange.FinalValue, Is.EqualTo(59));
            Assert.That(second.PressureChange.PreviousBand, Is.EqualTo(PressureBand.Tension));
            Assert.That(second.PressureChange.ResultingBand, Is.EqualTo(PressureBand.Distortion));
            Assert.That(second.PressureChange.ChangedBand, Is.True);
            Assert.That(third.PressureChange.RequestedDelta, Is.EqualTo(int.MaxValue));
            Assert.That(third.PressureChange.AppliedDelta, Is.EqualTo(41));
            Assert.That(third.PressureChange.FinalValue, Is.EqualTo(100));
            Assert.That(state.Pressure, Is.EqualTo(100));
        }

        [Test]
        public void PressureReductionClampsAtZeroWithoutIntegerOverflow()
        {
            PsychologyState state = new PsychologyState();
            state.ApplyPressure(new PressureDelta(PressureChangeCause.VoluntaryHit, 10));

            PressureChangeResult result = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.It04CigaretteBoxUsed, int.MinValue));

            Assert.That(result.PressureChange.PreviousValue, Is.EqualTo(10));
            Assert.That(result.PressureChange.RequestedDelta, Is.EqualTo(int.MinValue));
            Assert.That(result.PressureChange.AppliedDelta, Is.EqualTo(-10));
            Assert.That(result.PressureChange.FinalValue, Is.Zero);
            Assert.That(result.PressureChange.ResultingBand, Is.EqualTo(PressureBand.Control));
            Assert.That(state.Pressure, Is.Zero);
        }

        [Test]
        public void FirstEntryIntoRuptureAtomicallyAppliesTheUniqueLucidityPenalty()
        {
            PsychologyState state = new PsychologyState();
            state.AdjustLucidity(new LucidityDelta(LucidityAdjustmentCause.It04CigaretteBoxUsed, -60));

            PressureChangeResult result = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.Sc05DealerDealAccepted, 100));

            Assert.That(state.Pressure, Is.EqualTo(100));
            Assert.That(state.PressureBand, Is.EqualTo(PressureBand.Rupture));
            Assert.That(state.PressureRuptureTriggered, Is.True);
            Assert.That(state.Lucidity, Is.EqualTo(15));
            Assert.That(result.DidTriggerRupture, Is.True);
            Assert.That(result.Rupture.Pressure, Is.EqualTo(100));
            Assert.That(result.Rupture.LucidityPenalty, Is.EqualTo(-25));
            Assert.That(result.RuptureLucidityChange.PreviousValue, Is.EqualTo(40));
            Assert.That(result.RuptureLucidityChange.RequestedDelta, Is.EqualTo(-25));
            Assert.That(result.RuptureLucidityChange.FinalValue, Is.EqualTo(15));
            Assert.That(result.RuptureLucidityChange.Cause, Is.EqualTo(LucidityChangeCause.PressureRupture));
            Assert.That(result.RuptureLucidityChange.ResultingBand, Is.EqualTo(LucidityBand.Confused));
        }

        [Test]
        public void RuptureLucidityPenaltyClampsAtZero()
        {
            PsychologyState state = new PsychologyState();
            state.AdjustLucidity(new LucidityDelta(LucidityAdjustmentCause.Ev03DistractedResolved, -80));

            PressureChangeResult result = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.Sc04ThirdEyeResolved, 100));

            Assert.That(state.Lucidity, Is.Zero);
            Assert.That(result.RuptureLucidityChange.AppliedDelta, Is.EqualTo(-20));
            Assert.That(result.RuptureLucidityChange.RequestedDelta, Is.EqualTo(-25));
            Assert.That(result.RuptureLucidityChange.ResultingBand, Is.EqualTo(LucidityBand.Critical));
        }

        [Test]
        public void RuptureCannotTriggerAgainAfterPressureFallsAndReturnsToOneHundred()
        {
            PsychologyState state = new PsychologyState();
            PressureChangeResult firstEntry = state.ApplyPressure(
                new PressureDelta(PressureChangeCause.Sc05DealerDealAccepted, 100));
            state.ApplyPressure(PressureDelta.DefaultFor(PressureChangeCause.It04CigaretteBoxUsed));
            PressureChangeResult secondEntry = state.ApplyPressure(
                PressureDelta.DefaultFor(PressureChangeCause.AllInConfirmed));
            PressureChangeResult saturatedChange = state.ApplyPressure(
                PressureDelta.DefaultFor(PressureChangeCause.VoluntaryHit));

            Assert.That(firstEntry.DidTriggerRupture, Is.True);
            Assert.That(state.Pressure, Is.EqualTo(100));
            Assert.That(state.Lucidity, Is.EqualTo(75));
            Assert.That(secondEntry.DidTriggerRupture, Is.False);
            Assert.That(secondEntry.Rupture, Is.Null);
            Assert.That(secondEntry.RuptureLucidityChange, Is.Null);
            Assert.That(saturatedChange.DidTriggerRupture, Is.False);
            Assert.That(saturatedChange.PressureChange.ChangedValue, Is.False);
            Assert.That(saturatedChange.PressureChange.RequestedDelta, Is.EqualTo(10));
        }

        [Test]
        public void ReturnedPressureEventsRemainSnapshotsAfterLaterMutations()
        {
            PsychologyState state = new PsychologyState();
            PressureChanged firstEvent = state
                .ApplyPressure(PressureDelta.DefaultFor(PressureChangeCause.VoluntaryHit))
                .PressureChange;

            state.ApplyPressure(PressureDelta.DefaultFor(PressureChangeCause.AllInConfirmed));

            Assert.That(firstEvent.PreviousValue, Is.Zero);
            Assert.That(firstEvent.FinalValue, Is.EqualTo(10));
            Assert.That(firstEvent.ResultingBand, Is.EqualTo(PressureBand.Control));
            Assert.That(state.Pressure, Is.EqualTo(25));
        }

        [Test]
        public void NullPressureDeltaIsRejectedWithoutChangingState()
        {
            PsychologyState state = new PsychologyState();

            Assert.Throws<ArgumentNullException>(() => state.ApplyPressure(null));
            Assert.That(state.Pressure, Is.Zero);
            Assert.That(state.Lucidity, Is.EqualTo(100));
            Assert.That(state.PressureRuptureTriggered, Is.False);
        }
    }
}
