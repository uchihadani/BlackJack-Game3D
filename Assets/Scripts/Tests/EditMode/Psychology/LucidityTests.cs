using System;
using NUnit.Framework;
using TwentyThree.Domain.Psychology;

namespace TwentyThree.Tests.EditMode.Psychology
{
    public sealed class LucidityTests
    {
        [TestCase(0, LucidityBand.Critical, 0, 0, DistortionExpiration.HandResolution, DistortionSignal.None, -1)]
        [TestCase(1, LucidityBand.Confused, 1, 39, DistortionExpiration.NextValidPlayerCommand, DistortionSignal.None, -1)]
        [TestCase(39, LucidityBand.Confused, 1, 39, DistortionExpiration.NextValidPlayerCommand, DistortionSignal.None, -1)]
        [TestCase(40, LucidityBand.Unstable, 40, 69, DistortionExpiration.Timed, DistortionSignal.Subtle, 2000)]
        [TestCase(69, LucidityBand.Unstable, 40, 69, DistortionExpiration.Timed, DistortionSignal.Subtle, 2000)]
        [TestCase(70, LucidityBand.Clear, 70, 100, DistortionExpiration.Timed, DistortionSignal.Unmistakable, 750)]
        [TestCase(100, LucidityBand.Clear, 70, 100, DistortionExpiration.Timed, DistortionSignal.Unmistakable, 750)]
        public void EveryLucidityBoundaryHasItsExactDistortionProfile(
            int lucidity,
            LucidityBand expectedBand,
            int expectedMinimum,
            int expectedMaximum,
            DistortionExpiration expectedExpiration,
            DistortionSignal expectedSignal,
            int expectedDurationMilliseconds)
        {
            LucidityDistortionProfile profile =
                LucidityDistortionProfile.FromLucidity(lucidity);

            Assert.That(profile.Band, Is.EqualTo(expectedBand));
            Assert.That(profile.MinimumLucidity, Is.EqualTo(expectedMinimum));
            Assert.That(profile.MaximumLucidity, Is.EqualTo(expectedMaximum));
            Assert.That(profile.Expiration, Is.EqualTo(expectedExpiration));
            Assert.That(profile.Signal, Is.EqualTo(expectedSignal));

            if (expectedDurationMilliseconds < 0)
            {
                Assert.That(profile.FalseValueDuration, Is.Null);
            }
            else
            {
                Assert.That(
                    profile.FalseValueDuration.Value.TotalMilliseconds,
                    Is.EqualTo(expectedDurationMilliseconds));
            }
        }

        [TestCase(-1)]
        [TestCase(101)]
        public void LucidityProfilesRejectValuesOutsideInitialLimits(int lucidity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => LucidityDistortionProfile.FromLucidity(lucidity));
        }

        [TestCase(LucidityAdjustmentCause.It04CigaretteBoxUsed, -30)]
        [TestCase(LucidityAdjustmentCause.Ev02MiauResolved, -25)]
        [TestCase(LucidityAdjustmentCause.Ev03DistractedResolved, -30)]
        public void DefaultLucidityDeltasMatchTheTechnicalSheet(
            LucidityAdjustmentCause cause,
            int expectedPoints)
        {
            LucidityDelta delta = LucidityDelta.DefaultFor(cause);

            Assert.That(delta.Cause, Is.EqualTo(cause));
            Assert.That(delta.Points, Is.EqualTo(expectedPoints));
        }

        [Test]
        public void LucidityRequestsRejectInvalidValuesCausesAndDirections()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LucidityDelta((LucidityAdjustmentCause)999, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LucidityDelta(LucidityAdjustmentCause.Ev02MiauResolved, 1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LuciditySetRequest((LuciditySetCause)999, 40));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LuciditySetRequest(LuciditySetCause.It03SecondChanceActivated, -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LuciditySetRequest(LuciditySetCause.It03SecondChanceActivated, 101));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LucidityMaximumReduction((LucidityMaximumReductionCause)999, 70));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LucidityMaximumReduction(
                    LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                    -1));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LucidityMaximumReduction(
                    LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                    101));
        }

        [Test]
        public void LucidityAdjustmentClampsAtZeroAndReportsRequestedAndAppliedDeltas()
        {
            PsychologyState state = new PsychologyState();
            state.AdjustLucidity(new LucidityDelta(LucidityAdjustmentCause.Ev03DistractedResolved, -90));

            LucidityChanged change = state.AdjustLucidity(
                LucidityDelta.DefaultFor(LucidityAdjustmentCause.Ev02MiauResolved));

            Assert.That(change.PreviousValue, Is.EqualTo(10));
            Assert.That(change.RequestedDelta, Is.EqualTo(-25));
            Assert.That(change.AppliedDelta, Is.EqualTo(-10));
            Assert.That(change.FinalValue, Is.Zero);
            Assert.That(change.CurrentMaximum, Is.EqualTo(100));
            Assert.That(change.Cause, Is.EqualTo(LucidityChangeCause.Ev02MiauResolved));
            Assert.That(change.PreviousBand, Is.EqualTo(LucidityBand.Confused));
            Assert.That(change.ResultingBand, Is.EqualTo(LucidityBand.Critical));
            Assert.That(change.ChangedBand, Is.True);
            Assert.That(state.Lucidity, Is.Zero);
        }

        [Test]
        public void SecondChanceSetsLucidityExactlyToFortyInEitherDirection()
        {
            PsychologyState state = new PsychologyState();

            LucidityChanged lowered = state.SetLucidity(LuciditySetRequest.SecondChanceDefault());
            state.AdjustLucidity(LucidityDelta.DefaultFor(LucidityAdjustmentCause.It04CigaretteBoxUsed));
            LucidityChanged raised = state.SetLucidity(LuciditySetRequest.SecondChanceDefault());

            Assert.That(lowered.PreviousValue, Is.EqualTo(100));
            Assert.That(lowered.RequestedDelta, Is.EqualTo(-60));
            Assert.That(lowered.FinalValue, Is.EqualTo(40));
            Assert.That(lowered.Cause, Is.EqualTo(LucidityChangeCause.It03SecondChanceActivated));
            Assert.That(raised.PreviousValue, Is.EqualTo(10));
            Assert.That(raised.RequestedDelta, Is.EqualTo(30));
            Assert.That(raised.FinalValue, Is.EqualTo(40));
            Assert.That(state.Lucidity, Is.EqualTo(40));
        }

        [Test]
        public void ReducingMaximumClampsCurrentLucidityWithinTheSameResult()
        {
            PsychologyState state = new PsychologyState();

            LucidityMaximumChangeResult result = state.ReduceLucidityMaximum(
                LucidityMaximumReduction.SurrenderedEyeDefault());

            Assert.That(state.LucidityMaximum, Is.EqualTo(70));
            Assert.That(state.Lucidity, Is.EqualTo(70));
            Assert.That(result.MaximumChange.PreviousMaximum, Is.EqualTo(100));
            Assert.That(result.MaximumChange.RequestedMaximum, Is.EqualTo(70));
            Assert.That(result.MaximumChange.FinalMaximum, Is.EqualTo(70));
            Assert.That(result.MaximumChange.FinalLucidity, Is.EqualTo(70));
            Assert.That(result.MaximumChange.Cause, Is.EqualTo(LucidityMaximumReductionCause.Sc05DealerDealAccepted));
            Assert.That(result.MaximumChange.ChangedMaximum, Is.True);
            Assert.That(result.DidClampLucidity, Is.True);
            Assert.That(result.LucidityChange.PreviousValue, Is.EqualTo(100));
            Assert.That(result.LucidityChange.RequestedDelta, Is.EqualTo(-30));
            Assert.That(result.LucidityChange.FinalValue, Is.EqualTo(70));
            Assert.That(result.LucidityChange.CurrentMaximum, Is.EqualTo(70));
            Assert.That(result.LucidityChange.Cause, Is.EqualTo(LucidityChangeCause.Sc05DealerDealAccepted));
        }

        [Test]
        public void MaximumReductionIsMonotonicAndIdempotent()
        {
            PsychologyState state = new PsychologyState();
            state.AdjustLucidity(new LucidityDelta(LucidityAdjustmentCause.It04CigaretteBoxUsed, -40));
            LucidityMaximumReduction defaultReduction = LucidityMaximumReduction.SurrenderedEyeDefault();

            LucidityMaximumChangeResult first = state.ReduceLucidityMaximum(defaultReduction);
            LucidityMaximumChangeResult second = state.ReduceLucidityMaximum(defaultReduction);
            LucidityMaximumChangeResult attemptedIncrease = state.ReduceLucidityMaximum(
                new LucidityMaximumReduction(
                    LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                    90));

            Assert.That(first.MaximumChange.ChangedMaximum, Is.True);
            Assert.That(first.DidClampLucidity, Is.False);
            Assert.That(second.MaximumChange.ChangedMaximum, Is.False);
            Assert.That(second.DidClampLucidity, Is.False);
            Assert.That(attemptedIncrease.MaximumChange.ChangedMaximum, Is.False);
            Assert.That(attemptedIncrease.MaximumChange.RequestedMaximum, Is.EqualTo(90));
            Assert.That(state.LucidityMaximum, Is.EqualTo(70));
            Assert.That(state.Lucidity, Is.EqualTo(60));
        }

        [Test]
        public void LucidityCannotExceedReducedMaximumWhenSet()
        {
            PsychologyState state = new PsychologyState();
            state.ReduceLucidityMaximum(
                new LucidityMaximumReduction(
                    LucidityMaximumReductionCause.Sc05DealerDealAccepted,
                    30));

            LucidityChanged change = state.SetLucidity(
                new LuciditySetRequest(LuciditySetCause.It03SecondChanceActivated, 40));

            Assert.That(change.RequestedDelta, Is.EqualTo(10));
            Assert.That(change.AppliedDelta, Is.Zero);
            Assert.That(change.FinalValue, Is.EqualTo(30));
            Assert.That(state.Lucidity, Is.EqualTo(30));
            Assert.That(state.LucidityMaximum, Is.EqualTo(30));
        }

        [Test]
        public void NullLucidityRequestsAreRejectedWithoutMutation()
        {
            PsychologyState state = new PsychologyState();

            Assert.Throws<ArgumentNullException>(() => state.AdjustLucidity(null));
            Assert.Throws<ArgumentNullException>(() => state.SetLucidity(null));
            Assert.Throws<ArgumentNullException>(() => state.ReduceLucidityMaximum(null));
            Assert.That(state.Lucidity, Is.EqualTo(100));
            Assert.That(state.LucidityMaximum, Is.EqualTo(100));
        }
    }
}
